Shader "Custom/ResponsiveGrid"
{
    Properties
    {
        _GridSpacing ("Grid Spacing (world units)", Float) = 1.5
        _LineWidth ("Line Width", Float) = 0.006
        _InsideColor ("Inside Color (HDR)", Color) = (0.0, 1.94, 2.0, 1)
        _OutsideColor ("Outside Color (HDR)", Color) = (2.0, 0.25, 0.2, 1)
        _InsideIntensity ("Inside Intensity", Float) = 0.001
        _OutsideIntensity ("Outside Intensity", Float) = 0.09
        _OutsideAreaTint ("Outside Area Tint Intensity", Float) = 0.05
        _PulseInsideColor ("Pulse Color inside (HDR)", Color) = (0.0, 1.94, 2.0, 1)
        _PulseOutsideColor ("Pulse Color outside (HDR)", Color) = (2.0, 0.5, 0.3, 1)
        _PulseWidth ("Pulse Ring Width", Float) = 1.5
        _PulseIntensity ("Pulse Intensity", Float) = 1.8
        _PulseWhiten ("Pulse Whitening", Range(0, 1)) = 1
        _PulseNoise ("Pulse Noise Amount", Range(0, 1)) = 0.55
        _SpotRadius ("Block Glow Radius", Float) = 1.1
        _SpotIntensity ("Block Glow Intensity", Float) = 0.3
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend One One   // additive: black contributes nothing
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_PULSES 16
            #define MAX_SPOTS 64

            float _GridSpacing;
            float _LineWidth;
            float4 _InsideColor;
            float4 _OutsideColor;
            float _InsideIntensity;
            float _OutsideIntensity;
            float _OutsideAreaTint;
            float4 _PulseInsideColor;
            float4 _PulseOutsideColor;
            float _PulseWidth;
            float _PulseIntensity;
            float _PulseWhiten;
            float _PulseNoise;
            float _SpotRadius;
            float _SpotIntensity;

            // Set from C# every frame
            float4 _MapCenter;              // xy = world position of map center
            float _MapRadius;               // world units
            float4 _Pulses[MAX_PULSES];     // xy = origin, z = ring radius, w = strength (0 = inactive)
            float _PulseSeeds[MAX_PULSES];  // per-pulse random seed for noise variation
            float4 _Spots[MAX_SPOTS];       // xy = position, z = breathing flicker, w = fade-in
            float4 _SpotAnim[MAX_SPOTS];    // xy = arm blob offsets (radius units), z = arm strength
            int _SpotCount;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Smoothly interpolated value noise: unlike a per-cell hash there are
            // no hard brightness steps at cell boundaries.
            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.world.xy;

                // Distance to the nearest grid line (both axes), in world units.
                float spacing = max(_GridSpacing, 0.01);
                float2 cell = abs(frac(p / spacing) - 0.5) * spacing;
                float distToLine = min(cell.x, cell.y);

                // Inside vs outside the map circle. Hard cut at the border (just
                // one pixel of anti-aliasing) -- the border line itself marks the
                // boundary, so there is no gradual transition zone.
                float d = distance(p, _MapCenter.xy);
                float edgeAA = fwidth(d);
                float inside = 1.0 - smoothstep(_MapRadius - edgeAA, _MapRadius + edgeAA, d);

                float3 baseCol = lerp(_OutsideColor.rgb * _OutsideIntensity,
                                      _InsideColor.rgb * _InsideIntensity,
                                      inside);

                // Expanding glow rings with smooth spatial noise so some regions
                // light up more than others (uneven, organic ripple).
                float pulse = 0.0;
                [unroll]
                for (int k = 0; k < MAX_PULSES; k++)
                {
                    float4 pu = _Pulses[k];
                    if (pu.w <= 0.0001)
                        continue;

                    float pd = distance(p, pu.xy);

                    // The ring disperses like a water ripple: it widens (and thereby
                    // thins out) as it travels away from its origin.
                    float width = max(_PulseWidth, 0.001) * (1.0 + pu.z * 0.12);
                    float x = (pd - pu.z) / width;

                    // Asymmetric profile: crisp wavefront ahead, soft glowing wake
                    // trailing behind -- the signature of a fluid ripple.
                    float ring = (x > 0.0) ? exp(-x * x * 6.0) : exp(-x * x * 1.2);

                    float n = vnoise(p * 0.7 + _PulseSeeds[k]);
                    float noiseMod = lerp(1.0 - _PulseNoise, 1.0, n);

                    pulse += ring * pu.w * noiseMod;
                }

                // Standing glow under each block: a breathing gaussian core, plus
                // wandering blobs of glow that slide outward along the horizontal
                // and vertical grid lines through the block -- the glow feels alive
                // rather than a static disc.
                float spotR2 = max(_SpotRadius * _SpotRadius, 0.0001);
                float armW = max(_SpotRadius * 0.25, 0.01);
                float armL = max(_SpotRadius * 1.3, 0.01);
                [loop]
                for (int s = 0; s < _SpotCount; s++)
                {
                    float4 spot = _Spots[s];
                    float4 anim = _SpotAnim[s];
                    float2 sp = p - spot.xy;

                    float core = exp(-dot(sp, sp) / spotR2) * spot.z;

                    // Blob gliding along the horizontal line through the block.
                    float hx = sp.x - anim.x * _SpotRadius;
                    float armH = exp(-(sp.y * sp.y) / (armW * armW))
                               * exp(-(hx * hx) / (armL * armL));
                    // Blob gliding along the vertical line.
                    float vy = sp.y - anim.y * _SpotRadius;
                    float armV = exp(-(sp.x * sp.x) / (armW * armW))
                               * exp(-(vy * vy) / (armL * armL));

                    pulse += (core + (armH + armV) * anim.z) * _SpotIntensity * spot.w;
                }

                // Emphasize the hottest spots: brightness rises superlinearly with
                // pulse energy, and those places also bleach toward white, so the
                // noisy bright patches visibly glow harder than the rest of the ring.
                float energy = pulse * (1.0 + pulse);

                // Lines swell where a pulse is passing: up to 8x their natural
                // thickness at full glow, back to a hairline at rest. The soft
                // exponential (instead of a hard clamp) means the swell eases in
                // and out with no visible step as the glow fades.
                float swell = 1.0 - exp(-energy * 1.2);
                float lineWidth = _LineWidth * (1.0 + swell * 7.0);
                float aa = fwidth(distToLine);
                float gridMask = 1.0 - smoothstep(lineWidth - aa, lineWidth + aa, distToLine);

                float3 pulseCol = lerp(_PulseOutsideColor.rgb, _PulseInsideColor.rgb, inside);
                // Soft-saturating whiten for the same reason: a hard saturate() sits
                // pinned at the cap and then visibly "unclamps" while fading.
                float whiten = 1.0 - exp(-energy * 2.0 * _PulseWhiten);
                pulseCol = lerp(pulseCol, float3(2.5, 2.5, 2.5), whiten);
                float3 col = (baseCol + pulseCol * energy * _PulseIntensity) * gridMask;

                // Flat red wash over the whole out-of-bounds area (lines and cells
                // alike) so leaving the map reads as entering a hostile zone.
                col += _OutsideColor.rgb * _OutsideAreaTint * (1.0 - inside);

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}

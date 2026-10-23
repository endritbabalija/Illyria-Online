// Oak Valley sky (OakValleySky.mat), painted after the key art in docs/art/oak-valley/: a zenith-to-horizon
// gradient, a warm glow along the horizon on the sun's side, the sun's disc and halo, a band of soft procedural
// clouds, and two layers of distant ranges standing in the haze beyond the valley's own mountains. The bottom
// fades into the fog colour, so far terrain (fogged out at fogEnd) meets the sky without a seam. Pure ALU, no
// textures; the cloud noise and the ranges only run in the bands where they can show. Wuxia.World.ZoneAtmosphere
// drives the colours per area. The sun direction is URP's main light.
Shader "Wuxia/Sky"
{
    Properties
    {
        _ZenithColor("Zenith", Color) = (0.30, 0.46, 0.78, 1)
        _HorizonColor("Horizon", Color) = (0.98, 0.80, 0.64, 1)
        _SunGlowColor("Sun glow", Color) = (1, 0.62, 0.32, 1)
        _SunDisc("Sun disc", Range(0, 1)) = 1
        _SunSize("Sun size (radians)", Range(0.005, 0.2)) = 0.045
        _CloudColor("Clouds", Color) = (1, 0.82, 0.74, 1)
        _CloudCover("Cloud cover", Range(0, 1)) = 0.45
        _CloudScale("Cloud scale", Float) = 1.6
        _CloudSpeed("Cloud drift", Float) = 0.004
        _RidgeFarColor("Far ranges", Color) = (0.72, 0.70, 0.74, 1)
        _RidgeNearColor("Near ranges", Color) = (0.55, 0.56, 0.62, 1)
        _RidgeHeight("Range height", Float) = 1
        _HorizonFog("Horizon fog", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ZenithColor, _HorizonColor, _SunGlowColor, _CloudColor, _RidgeFarColor, _RidgeNearColor;
                float _SunDisc, _SunSize, _CloudCover, _CloudScale, _CloudSpeed, _RidgeHeight, _HorizonFog;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            // Hash without trig (stable at mobile precision), value noise, 3-octave fbm.
            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float v = Noise(p) * 0.55;
                p = p * 2.03 + float2(17.1, 3.7);
                v += Noise(p) * 0.3;
                p = p * 2.07 + float2(5.3, 11.9);
                v += Noise(p) * 0.15;
                return v;
            }

            // Silhouette of a mountain range around the horizon (integer frequencies so it wraps at ±pi).
            float Ridge(float azimuth, float seed)
            {
                float r = 0.40 * sin(azimuth * 3.0 + seed);
                r += 0.25 * sin(azimuth * 7.0 + seed * 2.3);
                r += 0.22 * (1.0 - abs(sin(azimuth * 11.0 + seed * 1.7)));
                r += 0.12 * (1.0 - abs(sin(azimuth * 23.0 + seed * 3.1)));
                r += 0.05 * sin(azimuth * 53.0 + seed);
                return r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float3 toSun = _MainLightPosition.xyz;
                float y = d.y;
                float up = saturate(y);
                float sunDot = dot(d, toSun);
                float sunSide = saturate(sunDot * 0.5 + 0.5);
                float facing = saturate(sunDot);

                // Gradient and the warm band along the horizon, strongest towards the sun.
                float3 col = lerp(_HorizonColor.rgb, _ZenithColor.rgb, 1.0 - pow(1.0 - up, 3.0));
                float band = exp(-up * 7.0);
                col = lerp(col, _SunGlowColor.rgb, band * pow(sunSide, 3.0) * 0.75);
                col += _SunGlowColor.rgb * (pow(facing, 40.0) * 0.45 + pow(facing, 400.0) * 1.2) * _SunDisc;

                // Clouds on a flat layer, thinning out towards the zenith and the horizon (the noise is
                // skipped where there can't be any: the sky is large and coherent, so the branch is cheap).
                float cover = 0.0;
                UNITY_BRANCH
                if (y > 0.02 && y < 0.8)
                {
                    float2 cloudUV = d.xz / (up + 0.18) * _CloudScale + _Time.y * _CloudSpeed;
                    float n = Fbm(cloudUV);
                    cover = smoothstep(1.0 - _CloudCover, 1.18 - _CloudCover * 0.6, n);
                    cover *= smoothstep(0.02, 0.10, y) * (1.0 - smoothstep(0.35, 0.8, y));
                    float3 cloud = _CloudColor.rgb * lerp(1.08, 0.78, saturate((n - 0.5) * 2.5));
                    cloud += _SunGlowColor.rgb * pow(facing, 6.0) * 0.6;
                    col = lerp(col, cloud, cover * 0.9);
                }

                // The sun's disc, behind the clouds; HDR so bloom picks it up.
                float disc = smoothstep(cos(_SunSize), cos(_SunSize * 0.75), sunDot) * _SunDisc * (1.0 - cover);
                col = lerp(col, float3(1.0, 0.96, 0.86) * 6.0, disc);

                // Distant ranges in the haze: a pale far layer and a darker near one, misty at their feet
                // (only near the horizon; the tallest peak stays below 0.12).
                UNITY_BRANCH
                if (y < 0.12 * _RidgeHeight)
                {
                    float azimuth = atan2(d.x, d.z);
                    float farTop = (0.030 + 0.045 * Ridge(azimuth, 1.3)) * _RidgeHeight;
                    float nearTop = (0.012 + 0.032 * Ridge(azimuth + 0.9, 4.1)) * _RidgeHeight;
                    float aa = 0.0015;
                    float3 haze = lerp(_HorizonColor.rgb, _SunGlowColor.rgb, pow(sunSide, 3.0) * 0.5);
                    float3 farCol = lerp(_RidgeFarColor.rgb, haze, saturate(1.0 - y / max(farTop, 0.001)) * 0.55);
                    col = lerp(col, farCol, smoothstep(farTop + aa, farTop - aa, y));
                    float3 nearCol = lerp(_RidgeNearColor.rgb, haze, saturate(1.0 - y / max(nearTop, 0.001)) * 0.6);
                    col = lerp(col, nearCol, smoothstep(nearTop + aa, nearTop - aa, y));
                }

                // Into the fog at the horizon and below it.
                col = lerp(col, unity_FogColor.rgb, saturate(_HorizonFog * exp(-max(y, 0.0) * 30.0)));
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

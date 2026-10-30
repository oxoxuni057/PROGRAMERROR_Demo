// 아날로그 TV 효과 (화면 전체): RGB 가로 어긋남, 주사선, 흐르는 밝은 띠, 가끔 가로로 찢어짐
// Renderer2D의 Full Screen Pass Renderer Feature에 AnalogTV 재질을 넣어서 사용
// 세기는 ScreenFilter가 전역 값 _AnalogTV(0~1)로 조절 (0이면 아무 효과 없음)
Shader "PROGRAMERROR/AnalogTV"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "AnalogTV"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _AnalogTV;                       // 세기 (ScreenFilter가 넣어 줌)
            SAMPLER(s_linear_clamp_sampler);

            float Hash(float n) { return frac(sin(n) * 43758.5453); }

            half3 Sample(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, s_linear_clamp_sampler, uv).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float k = saturate(_AnalogTV);
                if (k <= 0.001) return half4(Sample(uv), 1);

                float t = _Time.y;

                // ① 가끔 몇 줄이 옆으로 찢어지듯 밀림
                float bandY = Hash(floor(t * 1.7));
                float inBand = step(abs(uv.y - bandY), 0.015 + 0.03 * Hash(floor(t * 3.1)));
                float burst = step(0.7, Hash(floor(t * 6.0)));          // 늘 찢어지면 정신없어서 가끔만
                uv.x += inBand * burst * (Hash(floor(t * 30.0)) - 0.5) * 0.06 * k;

                // ② 화면 전체가 아주 살짝 출렁
                uv.x += sin(uv.y * 240.0 + t * 40.0) * 0.0007 * k;

                // ③ RGB 가로 어긋남 (빨강은 오른쪽, 파랑은 왼쪽, 가끔 덜덜)
                float split = (0.0025 + 0.003 * Hash(floor(t * 12.0))) * k;
                half3 col;
                col.r = Sample(uv + float2(split, 0)).r;
                col.g = Sample(uv).g;
                col.b = Sample(uv - float2(split, 0)).b;

                // ④ 주사선 (가는 가로줄)
                float scan = 0.5 + 0.5 * sin(uv.y * _ScreenParams.y * PI * 0.5);
                col *= lerp(1.0, 0.72 + 0.28 * scan, k);

                // ⑤ 위에서 아래로 천천히 흐르는 밝은 띠
                float roll = frac(uv.y + t * 0.07);
                col += smoothstep(0.0, 0.06, roll) * smoothstep(0.12, 0.06, roll) * 0.07 * k;

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}

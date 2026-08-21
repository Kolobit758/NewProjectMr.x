Shader "Custom/URP/FogOfWar_Final_URP"
{
    Properties
    {
        _MainTex ("Fog Texture (From C#)", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        // ตั้งค่าให้แสดงผลในหมวดโปร่งแสงและวาดทับฉากเกม 3D แน่นอน
        Tags 
        { 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent+100" 
            "RenderType"="Transparent" 
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float4 _FogColor;
            float4 _MainTex_TexelSize; // ใช้ในการหาขนาดพิกเซลข้างเคียงเพื่อทำขอบฟุ้ง

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // ดึงระดับความเข้มของหมอกจากช่องสีแดง (R Channel) ตามที่ C# ส่งมา
                float fogR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;
                
                // เบลอขอบ 4 ทิศทางในระดับ Shader เพื่อให้ขอบรอยเจาะนุ่มนวล ไม่เป็นเหลี่ยมพิกเซล
                float up    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, _MainTex_TexelSize.y)).r;
                float down  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, _MainTex_TexelSize.y)).r;
                float left  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(_MainTex_TexelSize.x, 0)).r;
                float right = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(_MainTex_TexelSize.x, 0)).r;

                // เฉลี่ยค่าความเข้มหมอก
                float finalFogFactor = (fogR + up + down + left + right) * 0.2;

                // คืนค่าเป็นสีที่เลือกจาก Inspector ร่วมกับความทึบของหมอกที่คำนวณได้
                return float4(_FogColor.rgb, finalFogFactor);
            }
            ENDHLSL
        }
    }
}
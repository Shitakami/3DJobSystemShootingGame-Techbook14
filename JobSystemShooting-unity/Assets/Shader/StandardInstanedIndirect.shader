Shader "Custom/InstancedSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;
        float3 _CenterOffset;

        struct Input
        {
            float2 uv_MainTex;
        };

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    StructuredBuffer<float4x4> _MatricesArray;
#endif

        float4x4 inverse4x4(float4x4 A)
        {
            float4x4 inv;
        
            inv[0] = A[1].yzwz * A[2].zwyx * A[3].wxyz -
                     A[1].wxyz * A[2].yzwz * A[3].zwyx;
        
            inv[1] = A[0].wxyz * A[2].yzwz * A[3].zwyx -
                     A[0].yzwz * A[2].zwyx * A[3].wxyz;
        
            inv[2] = A[0].yzwz * A[1].zwyx * A[3].wxyz -
                     A[0].wxyz * A[1].yzwz * A[3].zwyx;
        
            inv[3] = A[0].wxyz * A[1].yzwz * A[2].zwyx -
                     A[0].yzwz * A[1].zwyx * A[2].wxyz;
        
            float det = dot(A[0], inv[0]);
        
            if (abs(det) < 1e-8)
                return float4x4(1.0, 0.0, 0.0, 0.0,
                                0.0, 1.0, 0.0, 0.0,
                                0.0, 0.0, 1.0, 0.0,
                                0.0, 0.0, 0.0, 1.0);
        
            return inv * (1.0 / det);
        }

        
        void setup() {
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            unity_ObjectToWorld = _MatricesArray[unity_InstanceID];
            unity_ObjectToWorld._14_24_34 -= _CenterOffset;
            unity_WorldToObject = inverse4x4(unity_ObjectToWorld);
        #endif
        }
        
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 cy = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = cy.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = cy.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
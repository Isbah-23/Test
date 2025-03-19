Shader "Custom/ClippingShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Alpha ("Alpha", Range(0, 1)) = 1.0
        _AlphaClipThreshold ("Alpha Clip Threshold", Range(0, 1)) = 0.5
        _ClipPlane ("Clip Plane", Vector) = (0, 1, 0, -1.7) // Default clipping plane (x, y, z, w)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // Surface Shader definition
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        fixed4 _BaseColor; // For Albedo (orange)
        fixed4 _EmissionColor; // For emission (red)
        half _Metallic;
        half _Smoothness;
        float _Alpha;
        float _AlphaClipThreshold;
        float4 _ClipPlane; // Clipping plane (x, y, z, w)

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos; // World position for clipping and fresnel
            float3 viewDir; // View direction for fresnel
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Clipping based on the clipping plane
            if (dot(float4(IN.worldPos, 1.0), _ClipPlane) < 0)
            {
                discard; // Discard the pixel if it's outside the clipping plane
            }

            // Albedo (set base color to orange)
            o.Albedo = _BaseColor.rgb;

            // Set metallic and smoothness properties
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;

            // Fresnel Effect (calculated based on view direction)
            float fresnel = pow(1.0 - dot(normalize(IN.viewDir), normalize(IN.worldPos)), 3.0);

            // Emission: Multiply fresnel with emission color (Intensity of red glow)
            o.Emission = fresnel * _EmissionColor.rgb * 10.5; // Intensity = 3.5

            // Alpha and Alpha clipping (optional)
            o.Alpha = _Alpha;
            if (o.Alpha < _AlphaClipThreshold)
                discard;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
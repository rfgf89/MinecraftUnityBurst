Shader "Custom/MainShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _SizeVoxelX ("SizeVoxelX", float) = 1.0
        _SizeVoxelY ("SizeVoxelY", float) = 1.0 
        _SizeVoxelZ ("SizeVoxelZ", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float3 worldPos;
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
            float _SizeVoxelX;
            float _SizeVoxelY;
            float _SizeVoxelZ;
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v)
        {
            
        }

        float mip_map_level(in float2 texture_coordinate) // in texel units
        {
                float2 dx_vtc = ddx(texture_coordinate);
                float2 dy_vtc = ddy(texture_coordinate);
                float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));
                float mml = 0.5 * log2(delta_max_sqr);
                return max(0, mml); // Thanks @Nims
        }


        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
    
            float3 posWorld = (IN.worldPos.xyz - floor(IN.worldPos.xyz))*float3(_SizeVoxelX, _SizeVoxelY, _SizeVoxelZ);

            float2 uv;
     
            if(posWorld.x == 0.0){
              uv = posWorld.zy;
            }else if(posWorld.y == 0.0){
              uv = posWorld.xz;
            }else{
              uv = posWorld.xy;
            }

                float mipmapLevel = mip_map_level(uv*16);

                if (mipmapLevel > 4) {
                mipmapLevel = 4;
                }
            
            fixed4 c = tex2Dlod (_MainTex, float4((floor(uv.xy*16.0)/16.0) / 16.0 + IN.uv_MainTex, -0.5,  mipmapLevel)) ;
            
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
   
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0f;
            
        }
        ENDCG
    }
    FallBack "Diffuse"
}

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/MainNoShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SizeVoxelX ("SizeVoxelX", float) = 1.0
        _SizeVoxelY ("SizeVoxelY", float) = 1.0 
        _SizeVoxelZ ("SizeVoxelZ", float) = 1.0
        
        _PosX ("PosX", float) = 0.0
        _PosY ("PosY", float) = 0.0 
        _PosZ ("PosZ", float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                //int color : Normal;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 vertex1 : TEXCOORD1;
                //fixed4 color : COLOR;
                //float3 maxWHD : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _SizeVoxelX;
            float _SizeVoxelY;
            float _SizeVoxelZ;
            
            float _PosX;
            float _PosY;
            float _PosZ;
     
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex1 = v.vertex + float3(_PosX,_PosY,_PosZ);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
           
                //o.maxWHD = float3(((v.color >> 8) & 255), ((v.color >> 16) & 255), ((v.color >> 24) & 255));
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //o.color = fixed4((v.color & 3), ((v.color >> 2) & 3), ((v.color >> 4) & 3), ((v.color >> 6) & 3));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

        float3 biLerp(float3 a, float3 b, float3 c, float3 d, float s, float t)
        {
        float3 x = lerp(a, b, t);
        float3 y = lerp(c, d, t);
        return lerp(x, y, s);
        }
        float mip_map_level(in float2 texture_coordinate) // in texel units
        {
                float2 dx_vtc = ddx(texture_coordinate);
                float2 dy_vtc = ddy(texture_coordinate);
                float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));
                float mml = 0.5 * log2(delta_max_sqr);
                return max(0, mml); // Thanks @Nims
        }


            fixed4 frag (v2f i) : SV_Target
            {
            float3 pos = i.vertex1.xyz*float3(_SizeVoxelX, _SizeVoxelY, _SizeVoxelZ);
            float3 posWorld = (pos.xyz - floor(pos.xyz));
            
                
                
            float2 uv;
            //float2 uv3;
            //float3 normal;
            // Albedo comes from a texture tinted by color
            if(posWorld.x == 0.0){
              uv = posWorld.zy;
              //uv3 = ((i.vertex1.zy- (i.maxWHD.zy + float2(_PosZ,_PosY))))*float2(_SizeVoxelZ, _SizeVoxelY);
              //normal = float3(1.0,0.0,0.0);
            }else if(posWorld.y == 0.0){
              uv = posWorld.xz;
              //uv3 = ((i.vertex1.xz- (i.maxWHD.xz + float2(_PosX,_PosZ))))*float2(_SizeVoxelX, _SizeVoxelZ);
              //normal = float3(0.0,1.0,0.0);
            }else{
              uv = posWorld.xy;
              //uv3 = ((i.vertex1.xy- (i.maxWHD.xy + float2(_PosX,_PosY))))*float2(_SizeVoxelX, _SizeVoxelY);
              //normal = float3(0.0,0.0,1.0);
            }

                //float mipmapLevel = mip_map_level(uv*16);

                //if (mipmapLevel > 4) {
                //mipmapLevel = 4;
                //}
                
            
            
            
            float2 uv2 = (floor(uv.xy*16.0)/16.0) / 16.0 + i.uv;
            
            
            
            //float newColor = min((lerp(i.color.r,i.color.g,sin(uv3.x))+lerp(i.color.b,i.color.a,sin(uv3.y)))/2.0,1.0);
            //float newColor = min(biLerp(i.color.r, i.color.g, i.color.b, i.color.a, uv3.x, uv3.y), 1.0);
            fixed4 c = tex2Dlod (_MainTex, float4(uv2, -0.5,  0)) ;
            
            //float3 directional = 0.5 * (1 + dot(normalize(float3(0.2,0.1,0.5)), normal));
            
            //c *= half4(directional.x,directional.y,directional.z,0.0);
            
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}

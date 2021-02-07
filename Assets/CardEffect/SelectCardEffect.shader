//////////////////////////////////////////////////////////////
/// Shadero Sprite: Sprite Shader Editor - by VETASOFT 2018 //
/// Shader generate with Shadero 1.9.4                      //
/// http://u3d.as/V7t #AssetStore                           //
/// http://www.shadero.com #Docs                            //
//////////////////////////////////////////////////////////////

Shader "Shadero Customs/SelectCardEffect"
{
Properties
{
[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
_Color ("Tint", Color) = (1,1,1,1)
[HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
[HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
[PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
KaleidoscopeUV_PosX_1("KaleidoscopeUV_PosX_1",  Range(-2, 2)) = 0.5
KaleidoscopeUV_PosY_1("KaleidoscopeUV_PosY_1",  Range(-2, 2)) = 0.5
KaleidoscopeUV_Number_1("KaleidoscopeUV_Number_1", Range(0, 6)) = 1
_Generate_Fire_PosX_1("_Generate_Fire_PosX_1", Range(-1, 2)) = 0
_Generate_Fire_PosY_1("_Generate_Fire_PosY_1", Range(-1, 2)) = 0
_Generate_Fire_Precision_1("_Generate_Fire_Precision_1", Range(0, 1)) = 0.05
_Generate_Fire_Smooth_1("_Generate_Fire_Smooth_1", Range(0, 1)) = 0.5
_Generate_Fire_Speed_1("_Generate_Fire_Speed_1", Range(-2, 2)) = 1
_PlasmaLightFX_Fade_1("_PlasmaLightFX_Fade_1", Range(0, 1)) = 0.5
_PlasmaLightFX_Speed_1("_PlasmaLightFX_Speed_1", Range(0, 1)) = 0.5
_PlasmaLightFX_BW_1("_PlasmaLightFX_BW_1", Range(0, 1)) = 1
_TintRGBA_Color_1("_TintRGBA_Color_1", COLOR) = (1,1,1,1)
_SpriteFade("SpriteFade", Range(0, 1)) = 1.0

}

SubShader
{
Tags
{
"Queue" = "Transparent"
"IgnoreProjector" = "True"
"RenderType" = "Transparent"
"PreviewType" = "Plane"
"CanUseSpriteAtlas" = "True"

}

Cull Off
Lighting Off
ZWrite Off
Blend SrcAlpha OneMinusSrcAlpha


CGPROGRAM

#pragma surface surf Lambert vertex:vert  nolightmap nodynlightmap keepalpha noinstancing
#pragma multi_compile _ PIXELSNAP_ON
#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
#include "UnitySprites.cginc"
struct Input
{
float2 uv_MainTex;
float4 color;
};

float _SpriteFade;
float KaleidoscopeUV_PosX_1;
float KaleidoscopeUV_PosY_1;
float KaleidoscopeUV_Number_1;
float _Generate_Fire_PosX_1;
float _Generate_Fire_PosY_1;
float _Generate_Fire_Precision_1;
float _Generate_Fire_Smooth_1;
float _Generate_Fire_Speed_1;
float _PlasmaLightFX_Fade_1;
float _PlasmaLightFX_Speed_1;
float _PlasmaLightFX_BW_1;
float4 _TintRGBA_Color_1;

void vert(inout appdata_full v, out Input o)
{
v.vertex.xy *= _Flip.xy;
#if defined(PIXELSNAP_ON)
v.vertex = UnityPixelSnap (v.vertex);
#endif
UNITY_INITIALIZE_OUTPUT(Input, o);
o.color = v.color * _Color * _RendererColor;
}


float4 TintRGBA(float4 txt, float4 color)
{
float3 tint = dot(txt.rgb, float3(.222, .707, .071));
tint.rgb *= color.rgb;
txt.rgb = lerp(txt.rgb,tint.rgb,color.a);
return txt;
}
float Generate_Fire_hash2D(float2 x)
{
return frac(sin(dot(x, float2(13.454, 7.405)))*12.3043);
}

float Generate_Fire_voronoi2D(float2 uv, float precision)
{
float2 fl = floor(uv);
float2 fr = frac(uv);
float res = 1.0;
for (int j = -1; j <= 1; j++)
{
for (int i = -1; i <= 1; i++)
{
float2 p = float2(i, j);
float h = Generate_Fire_hash2D(fl + p);
float2 vp = p - fr + h;
float d = dot(vp, vp);
res += 1.0 / pow(d, 8.0);
}
}
return pow(1.0 / res, precision);
}

float4 Generate_Fire(float2 uv, float posX, float posY, float precision, float smooth, float speed, float black)
{
uv += float2(posX, posY);
float t = _Time*60*speed;
float up0 = Generate_Fire_voronoi2D(uv * float2(6.0, 4.0) + float2(0, -t), precision);
float up1 = 0.5 + Generate_Fire_voronoi2D(uv * float2(6.0, 4.0) + float2(42, -t ) + 30.0, precision);
float finalMask = up0 * up1  + (1.0 - uv.y);
finalMask += (1.0 - uv.y)* 0.5;
finalMask *= 0.7 - abs(uv.x - 0.5);
float4 result = smoothstep(smooth, 0.95, finalMask);
result.a = saturate(result.a + black);
return result;
}
float2 KaleidoscopeUV(float2 uv, float posx, float posy, float number)
{
uv = uv - float2(posx, posy);
float r = length(uv);
float a = abs(atan2(uv.y, uv.x));
float sides = number;
float tau = 3.1416;
a = fmod(a, tau / sides);
a = abs(a - tau / sides / 2.);
uv = r * float2(cos(a), sin(a));
return uv;
}
inline float RBFXmod2(float x,float modu)
{
return x - floor(x * (1.0 / modu)) * modu;
}

float3 RBFXrainbow2(float t)
{
t= RBFXmod2(t,1.0);
float tx = t * 8;
float r = clamp(tx - 4.0, 0.0, 1.0) + clamp(2.0 - tx, 0.0, 1.0);
float g = tx < 2.0 ? clamp(tx, 0.0, 1.0) : clamp(4.0 - tx, 0.0, 1.0);
float b = tx < 4.0 ? clamp(tx - 2.0, 0.0, 1.0) : clamp(6.0 - tx, 0.0, 1.0);
return float3(r, g, b);
}

float4 PlasmaLight(float4 txt, float2 uv, float _Fade, float speed, float bw)
{
float _TimeX=_Time.y * speed;
float a = 1.1 + _TimeX * 2.25;
float b = 0.5 + _TimeX * 1.77;
float c = 8.4 + _TimeX * 1.58;
float d = 610 + _TimeX * 2.03;
float x1 = 2.0 * uv.x;
float n = sin(a + x1) + sin(b - x1) + sin(c + 2.0 * uv.y) + sin(d + 5.0 * uv.y);
n = RBFXmod2(((5.0 + n) / 5.0), 1.0);
float4 nx=txt;
n += nx.r * 0.2 + nx.g * 0.4 + nx.b * 0.2;
float4 ret=float4(RBFXrainbow2(n),txt.a);
ret= lerp(ret,dot(ret.rgb,1),bw);
ret = lerp(txt,txt+ret,_Fade);
ret.a = txt.a;
return ret;
}
void surf(Input i, inout SurfaceOutput o)
{
float2 KaleidoscopeUV_1 = KaleidoscopeUV(i.uv_MainTex,KaleidoscopeUV_PosX_1,KaleidoscopeUV_PosY_1,KaleidoscopeUV_Number_1);
float4 _Generate_Fire_1 = Generate_Fire(KaleidoscopeUV_1,_Generate_Fire_PosX_1,_Generate_Fire_PosY_1,_Generate_Fire_Precision_1,_Generate_Fire_Smooth_1,_Generate_Fire_Speed_1,0);
float4 _PlasmaLightFX_1 = PlasmaLight(_Generate_Fire_1,i.uv_MainTex,_PlasmaLightFX_Fade_1,_PlasmaLightFX_Speed_1,_PlasmaLightFX_BW_1);
float4 TintRGBA_1 = TintRGBA(_PlasmaLightFX_1,_TintRGBA_Color_1);
float4 FinalResult = TintRGBA_1;
o.Albedo = FinalResult.rgb* i.color.rgb;
o.Alpha = FinalResult.a * _SpriteFade * i.color.a;
clip(o.Alpha - 0.05);
}

ENDCG
}
Fallback "Sprites /Default"
}

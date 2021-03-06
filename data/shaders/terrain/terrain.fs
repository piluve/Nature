#version  430 core
#extension GL_EXT_gpu_shader4 : enable

layout(std140)uniform uPass
{
    mat4 uView;
    mat4 uProjection;
    vec3 uCampos;
    float uTime;
    float uCamnear;
    float uCamfar;
};

uniform sampler2D uGrassTexture;
uniform sampler2D uCliffTexture;
uniform sampler2D uSnowTexture;
uniform sampler2D uSplatTexture;
uniform sampler2D uGrassNormTexture;
uniform sampler2D uCliffNormTexture;
uniform sampler2D uSnowNormTexture;
uniform sampler2D uHeightMap;
uniform sampler2D uLutTexture;
uniform sampler2D uNormalTexture;
uniform vec3 uSunPosition;
uniform float uTiling1;
uniform float uTiling2;
uniform float uNightAten;

in vec2 iTexcoord;
in vec3 iPosition;
in float iLogz;

out vec4 oColor;

vec4 Hash4( vec2 p ) 
{ 
	return fract(sin(vec4( 1.0+dot(p,vec2(37.0,17.0)), 
                                              2.0+dot(p,vec2(11.0,47.0)),
                                              3.0+dot(p,vec2(41.0,29.0)),
                                              4.0+dot(p,vec2(23.0,31.0))))*103.0); 
}

/*
	Value noise using lut texture [0,1]
*/
float VNoise( in vec3 x )
{
    vec3 p = floor(x.xzy);
    vec3 f = fract(x.xzy);
	vec3 f2 = f*f; 
	f = f*f2*(10.0-15.0*f+6.0*f2);
	vec2 uv = (p.xy+vec2(37.0,17.0)*p.z) + f.xy;
	vec2 rg = texture2D( uLutTexture, (uv+0.5)/256.0, -100.0 ).ba;
    float n = mix( rg.y, rg.x, f.z )-.5;
	return n + 1.0 * 0.5;
}

/*
	Fractional Brownian motion [0,1] 2 octaves
*/
float Fbm2(vec3 pos) 
{
	vec3 q = pos;
	float f;
    f  = 0.50000*VNoise( q ); q = q*2.02;
    f += 0.25000*VNoise( q );;
	return f;
}

float CloudsShadowing()
{
	vec2 w = vec2(0.05f,0.05f) * uTime;
	vec3 p = vec3(iPosition.x,250.0f,iPosition.z);
	p *= 0.0015f;
	p.xz += w;
	return (Fbm2(p)-1.0f)*-1.0f;
}

vec4 texture2DNoTile( sampler2D samp, in vec2 uv )
{
	vec2 iuv = floor( uv );
    vec2 fuv = fract( uv );

    // generate per-tile transform
    vec4 ofa = Hash4( iuv + vec2(0.0,0.0) );
    vec4 ofb = Hash4( iuv + vec2(1.0,0.0) );
    vec4 ofc = Hash4( iuv + vec2(0.0,1.0) );
    vec4 ofd = Hash4( iuv + vec2(1.0,1.0) );
    
    vec2 ddx = dFdx( uv );
    vec2 ddy = dFdy( uv );

    // transform per-tile uvs
    ofa.zw = sign(ofa.zw-0.5);
    ofb.zw = sign(ofb.zw-0.5);
    ofc.zw = sign(ofc.zw-0.5);
    ofd.zw = sign(ofd.zw-0.5);
    
    // uv's, and derivarives (for correct mipmapping)
    vec2 uva = uv*ofa.zw + ofa.xy; vec2 ddxa = ddx*ofa.zw; vec2 ddya = ddy*ofa.zw;
    vec2 uvb = uv*ofb.zw + ofb.xy; vec2 ddxb = ddx*ofb.zw; vec2 ddyb = ddy*ofb.zw;
    vec2 uvc = uv*ofc.zw + ofc.xy; vec2 ddxc = ddx*ofc.zw; vec2 ddyc = ddy*ofc.zw;
    vec2 uvd = uv*ofd.zw + ofd.xy; vec2 ddxd = ddx*ofd.zw; vec2 ddyd = ddy*ofd.zw;
        
    // fetch and blend
    vec2 b = smoothstep(0.25,0.75,fuv);
    
    return mix( mix( texture2DGrad( samp, uva, ddxa, ddya ), 
                     texture2DGrad( samp, uvb, ddxb, ddyb ), b.x ), 
                mix( texture2DGrad( samp, uvc, ddxc, ddyc ),
                     texture2DGrad( samp, uvd, ddxd, ddyd ), b.x), b.y );
}

vec3 GetBaseColor()
{
	vec3 splat = texture(uSplatTexture,iTexcoord).xyz;

	//vec3 grass = texture(uGrassTexture,iTexcoord * uTiling1).xyz;
	vec3 cliff = texture(uCliffTexture,iTexcoord * uTiling2).xyz;
    vec3 snow = texture(uSnowTexture,iTexcoord * uTiling1).xyz;
    
    vec3 finalColor = vec3(0.0f);
    //finalColor = mix(finalColor,grass,splat.g);
    //finalColor = mix(finalColor,cliff,splat.b);
    //finalColor = mix(finalColor,snow,splat.r);  
    return mix(cliff,snow,splat.x);
}

mat3 CotangentFrame(vec3 N, vec3 p, vec2 uv)
{
    // get edge vectors of the pixel triangle
    vec3 dp1 = dFdx( p );
    vec3 dp2 = dFdy( p );
    vec2 duv1 = dFdx( uv );
    vec2 duv2 = dFdy( uv );
 
    // solve the linear system
    vec3 dp2perp = cross( dp2, N );
    vec3 dp1perp = cross( N, dp1 );
    vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
    vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;
 
    // construct a scale-invariant frame 
    float invmax = inversesqrt( max( dot(T,T), dot(B,B) ) );
    return mat3( T * invmax, B * invmax, N );
}

vec3 GetNormal(vec3 curNormal,vec3 view)
{
    vec3 splat = texture(uSplatTexture,iTexcoord).xyz;

    //vec3 grassN = normalize(texture(uGrassNormTexture,iTexcoord * uTiling1).xyz * 2.0f - 1.0f);
    vec3 cliffN = normalize(texture(uCliffNormTexture,iTexcoord * uTiling2).xyz * 2.0f - 1.0f);
    vec3 snowN = normalize(texture(uSnowNormTexture,iTexcoord * uTiling1).xyz * 2.0f - 1.0f);

    vec3 finalNormal = vec3(0.0f);
    //finalNormal = mix(finalNormal,grassN,splat.g);
    //finalNormal = mix(finalNormal,cliffN,splat.b);
    //finalNormal = mix(finalNormal,snowN,splat.r);  
    finalNormal = mix(cliffN,snowN,splat.x);

    mat3 tbn = CotangentFrame(curNormal,view,iTexcoord);
    return normalize(tbn * finalNormal);
}

vec3 GetFog(vec3 c, float d,float h)
{
    const vec3 fogColor = vec3(0.0);
    float fogMod = clamp(d / 4000.0,0.0,1.0);
    fogMod *= pow(1.0 - (h /(200.0 * 9.0)),2.0);
    return mix(c,fogColor,fogMod);
}

const float PI = 3.14159265359;

vec3 FresnelSchlick(float cosTheta, vec3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}  

float DistributionGGX(vec3 N, vec3 H, float roughness)
{
    float a      = roughness*roughness;
    float a2     = a*a;
    float NdotH  = max(dot(N, H), 0.0);
    float NdotH2 = NdotH*NdotH;
    
    float nom   = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
    
    return nom / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r*r) / 8.0;

    float nom   = NdotV;
    float denom = NdotV * (1.0 - k) + k;
    
    return nom / denom;
}

float GeometrySmith(vec3 N, vec3 V, vec3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2  = GeometrySchlickGGX(NdotV, roughness);
    float ggx1  = GeometrySchlickGGX(NdotL, roughness);
    
    return ggx1 * ggx2;
}

void main()
{
    // Load normal and hack to work with world machine normals
    vec3 normal = texture(uNormalTexture,iTexcoord).xzy;
    normal = normalize(normal * 2.0f - 1.0f);
    normal.z *= -1.0f;
    normal = GetNormal(normal,normalize(uCampos - iPosition));

    // Shading
    float metallic = 0.0f;
    float roughness = 1.0;
    vec3 V = normalize(uCampos - iPosition);
    vec3 H = normalize(normalize(uSunPosition) + V);
    vec3 albedo = GetBaseColor();
    vec3 F0 = vec3(0.04); 
    F0 = mix(F0,albedo,metallic);
    vec3 F  = FresnelSchlick(max(dot(H, V), 0.0), F0);
    float NDF = DistributionGGX(normal, H, roughness);       
    float G   = GeometrySmith(normal, V, normalize(uSunPosition), roughness);  
    vec3 nominator    = NDF * G * F;
    float denominator = 4 * max(dot(normal, V), 0.0) * max(dot(normal, normalize(uSunPosition)), 0.0) + 0.001; 
    vec3 specular     = nominator / denominator; 

    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
  
    kD *= 1.0 - metallic;
  
    float NdotL = max(dot(normal, normalize(uSunPosition)), 0.2);        
    oColor.xyz = (kD * albedo / PI + specular) * vec3(1.0) * NdotL;
    oColor *= uNightAten;

    //float l = max(dot(normalize(normal),normalize(uSunPosition)),0.15f);
    //oColor = vec4(base * l,1.0f) * CloudsShadowing();
    //oColor *= uNightAten;
    
    // Fog
    //oColor.xyz = GetFog(oColor.xyz,distance(uCampos,iPosition),iPosition.y);

    // Logarithmic z-buffer
    float Fcoef_half = 0.5f * (2.0 / log2(uCamfar + 1.0));
    gl_FragDepth = log2(iLogz) * Fcoef_half;
}   



//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))
float4x4 matProj;			// Projection
float specularFactor = 3;


float screen_dx;					// tamaño de la pantalla en pixels
float screen_dy;


//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

//Textura para Lightmap
texture texLightMap;
sampler2D lightMap = sampler_state
{
	Texture = (texLightMap);
};


texture g_RenderTarget;
sampler RenderTarget =
sampler_state
{
	Texture = <g_RenderTarget>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

texture g_Position;
sampler PositionBuffer =
sampler_state
{
	Texture = <g_Position>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};


texture g_Normal;
sampler NormalBuffer =
sampler_state
{
	Texture = <g_Normal>;
	ADDRESSU = CLAMP;
	ADDRESSV = CLAMP;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};


texture texNoise;
sampler2D noise = sampler_state
{
	Texture = (texNoise);
	ADDRESSU = WRAP;
	ADDRESSV = WRAP;
	MINFILTER = POINT;
	MAGFILTER = POINT;
	MIPFILTER = POINT;
};

//Material del mesh
float3 materialEmissiveColor; //Color RGB
float3 materialAmbientColor; //Color RGB
float4 materialDiffuseColor; //Color ARGB (tiene canal Alpha)
float3 materialSpecularColor; //Color RGB
float materialSpecularExp; //Exponente de specular

//Parametros de la Luz
float3 lightColor; //Color RGB de la luz
float4 lightPosition; //Posicion de la luz
float4 eyePosition; //Posicion de la camara
float lightIntensity; //Intensidad de la luz
float lightAttenuation; //Factor de atenuacion de la luz
float3 lightDir; 		//Luz direccional

struct VS_INPUT
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float4 Color : COLOR;
	float2 Texcoord : TEXCOORD0;
};

struct VS_OUTPUT
{
	float4 Position : POSITION0;
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	float3 ViewVec	: TEXCOORD3;
	float3 ViewPosition : TEXCOORD4;
	float3 ViewNormal : TEXCOORD5;
	float3 ModelPosition : TEXCOORD6;

};

VS_OUTPUT vs_main(VS_INPUT input)
{
	VS_OUTPUT output;
	output.Position = mul(input.Position, matWorldViewProj);
	output.Texcoord = input.Texcoord;
	output.WorldPosition = mul(input.Position, matWorld);
	output.ViewPosition = mul(input.Position, matWorldView);
	output.WorldNormal = mul(input.Normal, matInverseTransposeWorld).xyz;
	output.ViewNormal = mul(input.Normal, matWorldView);;
	output.ViewVec = eyePosition.xyz - output.WorldPosition;				//ViewVec (V): vector que va desde el vertice hacia la camara.
	output.ModelPosition = input.Position;
	return output;
}

struct PS_INPUT
{
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float3 WorldNormal : TEXCOORD2;
	float3 ViewVec	: TEXCOORD3;
	float3 ViewPosition : TEXCOORD4;
	float3 ViewNormal : TEXCOORD5;
	float3 ModelPosition : TEXCOORD6;
	
};

//Pixel Shader
float4 ps_main(PS_INPUT input) : COLOR0
{
	float occ_factor = 0.5 + input.ModelPosition.y/10.0;
	float3 Nn = normalize(input.WorldNormal);
	float3 Ln = lightDir;
	float3 Vn = normalize(input.ViewVec);
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);
	float3 ambientLight = lightColor * materialAmbientColor * occ_factor;
	float3 n_dot_l = dot(Nn, Ln);
	float3 diffuseLight = lightColor * materialDiffuseColor.rgb * max(0.0, n_dot_l);
	float ks = saturate(dot(reflect(-Ln,Nn), Vn));
	float3 specularLight = specularFactor * lightColor * materialSpecularColor *pow(ks,materialSpecularExp);
	float4 finalColor = float4(saturate(materialEmissiveColor + ambientLight + diffuseLight) * texelColor + specularLight, materialDiffuseColor.a);
	
	return finalColor  * occ_factor;

	//return float4(specularLight , 1);
	//return float4(Nn , 1);
	//return finalColor;

}



//Pixel Shader
float4 ps_position(PS_INPUT input) : COLOR0
{
	return float4(input.ViewPosition, 1);
}

float4 ps_normal(PS_INPUT input) : COLOR0
{
	float3 Nn = normalize(input.ViewNormal);
	return float4(Nn, 1);
}


technique DefaultTechnique
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_main();
	}
}

technique PositionMap
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_position();
	}
}

technique NormalMap
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 ps_normal();
	}
}


void VSCopy(float4 vPos : POSITION, float2 vTex : TEXCOORD0, out float4 oPos : POSITION, out float2 oScreenPos : TEXCOORD0)
{
	oPos = vPos;
	oScreenPos = vTex;
	oPos.w = 1;
}


const int samples = 16;
float3 sample_sphere[16] = {
      float3( 0.5381, 0.1856,-0.4319), float3( 0.1379, 0.2486, 0.4430),
      float3( 0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
      float3( 0.0689,-0.1598,-0.8547), float3( 0.0560, 0.0069,-0.1843),
      float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924,-0.0344),
      float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
      float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
      float3( 0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
      float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
};

float4 PSPostProcess(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) : COLOR0
{
	return tex2D(RenderTarget, Tex);
	
	/*
	float2 uNoiseScale = float2(1024 / 8, 768 / 8);
	float uRadius = 1;
	float3 origin = tex2D(PositionBuffer,Tex).xyz;
	float3 normal = tex2D(NormalBuffer, Tex).xyz;
	float3 rvec = tex2D(noise, Tex * uNoiseScale).xyz * 2.0 - 1.0;
	rvec.z= 0;
	float3 tangent = normalize(rvec - normal * dot(rvec, normal));
	//float3 tangent = float3(0,0,1);
	//if(dot(normal , tangent)>=0.999)
	//	tangent = float3(0,1,0);
	float3 bitangent = cross(normal, tangent);
	float3x3 tbn = float3x3(normal ,  tangent , bitangent);	
	
	float occlusion = 0.0;
	for (int i = 0; i < samples; ++i) 
	{
		//float3 sample = mul(float3(sample_sphere[i].xy , 0) , tbn);
		float3 sample = sample_sphere[i];
		sample = sample * uRadius + origin;
		float4 offset = float4(sample, 1.0);
	    offset = mul(offset , matProj);
		offset.xy /= offset.w;
		offset.xy = offset.xy * 0.5 + 0.5;
		if(offset.x>=0 && offset.x<=1 && offset.y>=0 && offset.y<=1)
		{
			float sampleDepth = tex2D(PositionBuffer, offset.xy).z;
			float rangeCheck= abs(origin.z - sampleDepth) < uRadius ? 1.0 : 0.0;
			occlusion += (sampleDepth <= sample.z ? 1.0 : 0.0);	// * rangeCheck;
		}
		else occlusion = 100;
	}	
	
	return occlusion >= 100 ? float4(1,0,1,1) : 1 - occlusion / (float)samples;
	*/
	
//	float4 ColorBase = tex2D(RenderTarget, Tex);
//	return ColorBase;
//	float fragment_z = tex2D(PositionBuffer, Tex).z;
}

technique PostProcess
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 PSPostProcess();
	}
}


/* edge detection
	float3 n0 = tex2D(NormalBuffer,Tex).xyz;
	float3 n1 = tex2D(NormalBuffer,Tex + float2(0.5 / screen_dx, 0.5 / screen_dy)).xyz;
	float d = cross(n0, n1).z;
	
	return d>0 ? 1 : dot(n0 , n1);
*/

/* SSAO

	float radio = 0.1;
	float3 pos = tex2D(PositionBuffer,Tex).xyz;
	float occlusion = 0.0;
	for(int i=0; i < samples; i++) 
	{
		float4 pos_sample = mul(float4(pos + radio*sample_sphere[i] , 1), matProj);
		float2 offset = pos_sample.xy / pos_sample.w; 
		offset.xy = offset.xy * 0.5 + 0.5;
		float sampleDepth = tex2D(PositionBuffer, offset.xy).z;
		float rangeCheck= abs(pos.z - sampleDepth) < radio ? 1.0 : 0.0;
		occlusion += (sampleDepth <= pos.z ? 1.0 : 0.0) * rangeCheck;
	}
	return 1 - occlusion / (float)samples;
	
*/
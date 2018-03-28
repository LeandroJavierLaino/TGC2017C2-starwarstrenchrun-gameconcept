//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))
float4x4 matProj;			// Projection
float specularFactor = 3;

static const float PI = 3.14159265f;

int ssao = 1;
float screen_dx;					// tamaño de la pantalla en pixels
float screen_dy;


//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
	Texture = (texDiffuseMap);
	ADDRESSU = MIRROR;
	ADDRESSV = MIRROR;
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

texture texDeathStarSurface;
sampler2D ds_surface = sampler_state
{
	Texture = (texDeathStarSurface);
	ADDRESSU = MIRROR;
	ADDRESSV = MIRROR;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
};

texture texSkybox;
sampler2D SkyBoxMap = sampler_state
{
	Texture = (texSkybox);
	ADDRESSU = MIRROR;
	ADDRESSV = MIRROR;
	MINFILTER = LINEAR;
	MAGFILTER = LINEAR;
	MIPFILTER = LINEAR;
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
	float4 WorldNormalOcc : TEXCOORD2;			// xyz = normal w = off factor
	float3 ViewVec	: TEXCOORD3;
	float3 LightVec	: TEXCOORD4;

};

VS_OUTPUT vs_main(VS_INPUT input)
{
	VS_OUTPUT output;
	output.Position = mul(input.Position, matWorldViewProj);
	output.Texcoord = input.Texcoord;
	output.WorldPosition = mul(input.Position, matWorld);
	output.ViewVec = eyePosition.xyz - output.WorldPosition;				//ViewVec (V): vector que va desde el vertice hacia la camara.
	output.LightVec = lightPosition.xyz - output.WorldPosition;
	// normal en worldspace
	output.WorldNormalOcc.xyz = mul(input.Normal, matInverseTransposeWorld).xyz;
	// factor de occlussion
	output.WorldNormalOcc.w = 0.5 + input.Position.y/10.0;
	return output;
}

struct PS_INPUT
{
	float2 Texcoord : TEXCOORD0;
	float3 WorldPosition : TEXCOORD1;
	float4 WorldNormalOcc : TEXCOORD2;
	float3 ViewVec	: TEXCOORD3;
	float3 LightVec	: TEXCOORD4;
};

//Pixel Shader
float4 ps_main(PS_INPUT input) : COLOR0
{
	float occ_factor = ssao ? input.WorldNormalOcc.w : 1;
	float3 Nn = normalize(input.WorldNormalOcc.xyz);
	float3 Ln = normalize(input.LightVec);
	//float3 Ln = lightDir;
	float3 Vn = normalize(input.ViewVec);
	float4 texelColor = tex2D(diffuseMap, input.Texcoord);
	float3 ambientLight = lightColor * materialAmbientColor * occ_factor;
	float3 n_dot_l = dot(Nn, Ln);
	float3 diffuseLight = lightColor * materialDiffuseColor.rgb * max(0.0, n_dot_l);
	float ks = saturate(dot(reflect(-Ln,Nn), Vn));
	float3 specularLight = specularFactor * lightColor * materialSpecularColor *pow(ks,materialSpecularExp);
	float4 finalColor = float4(saturate(materialEmissiveColor + ambientLight + diffuseLight) * texelColor + specularLight, materialDiffuseColor.a);
	return finalColor  * occ_factor;
}




float4 ps_normal(PS_INPUT input) : COLOR0
{
	float3 Nn = normalize(input.WorldNormalOcc.xyz);
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



float4 PSPostProcess(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) : COLOR0
{
	return tex2D(RenderTarget, Tex);
}

technique PostProcess
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 PSPostProcess();
	}
}


VS_OUTPUT vs_skybox(VS_INPUT input)
{
	VS_OUTPUT output = (VS_OUTPUT)0;
	output.Position = mul(input.Position, matWorldViewProj).xyww;
	//output.Position = mul(input.Position, matWorldViewProj);
	output.Texcoord = input.Texcoord;
	output.WorldPosition = mul(input.Position, matWorld);
	return output;
}





const float star_r = 2500;
const float r2 = 2510 * 2510;
const float3 reactor = float3(-2160,2160,0);
const float radio_reactor = 1100;


float2 sphere(float3 ro, float3 rd , float3 center, float radio)
{
	ro-=center;
	float c = dot(ro, ro) - radio*radio;
	float b = dot(rd, ro);
	float d = b*b - c;
	return d<0 ? float2(0,0) : float2 (-b - sqrt(d) , -b + sqrt(d));
}


float4 texCube_skybox(float3 d)
{
	float3 absd = abs(d);
	float s0 = 0; 
	float t0 = 0; 
	float s,t;
	float sc, tc, ma;

	if ((absd.x >= absd.y) && (absd.x >= absd.z)) 
	{
		if (d.x > 0.0f) 
		{
			// right
			s0 = 0.5 , t0 = 1.0/3.0;
			sc = -d.z; tc = -d.y; ma = absd.x;
		} 
		else 
		{
			// left
			s0 = 0 , t0 = 1.0/3.0;
			sc = d.z; tc = -d.y; ma = absd.x;
		}
	}
	if ((absd.y >= absd.x) && (absd.y >= absd.z)) 
	{
		if (d.y > 0.0f) 
		{
			// top
			s0 = 0.25 , t0 = 0;
			sc = d.x; tc = d.z; ma = absd.y;
		} 
		else 
		{
			// bottom
			s0 = 0.25 , t0 = 2.0/3.0;
			sc = d.x; tc = -d.z; ma = absd.y;
		}
	}
	if ((absd.z >= absd.x) && (absd.z >= absd.y)) 
	{
		if (d.z > 0.0f) 
		{
			// front
			s0 = 0.25 , t0 = 1.0/3.0;
			sc = d.x; tc = -d.y; ma = absd.z;
		} 
		else 
		{
			// back
			s0 = 0.75 , t0 = 1.0/3.0;
			sc = -d.x; tc = -d.y; ma = absd.z;
		}
	}

	if (ma == 0.0f) 
	{
		s = 0.0f;
		t = 0.0f;
	} 
	else 
	{
		s = ((sc / ma) + 1.0f) * 0.5f;
		t = ((tc / ma) + 1.0f) * 0.5f;
	}
	float ep = 0.01f;
	s = clamp(s , ep , 1-ep);
	t = clamp(t , ep , 1-ep);
	return tex2Dlod(SkyBoxMap, float4(s0+ s /4.0 ,t0+ t/3.0,0,0));
}

float3 LookFrom;
float3 ViewDir;
float3 Dx , Dy;
float MatProjQ, Zn,Zf;

struct PS_OUTPUT 
{
	float4 color : COLOR0;
	float depth : DEPTH0;
};


PS_OUTPUT ps_ds(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) 
{
	PS_OUTPUT rta;
	float x = vpos.x;
	float y = vpos.y;
	float3 rd = normalize(ViewDir + Dy*(0.5*(screen_dy-2*y)) - Dx*(0.5*(2*x-screen_dx)));	
	float t = sphere(LookFrom, rd ,float3(0,0,0) , star_r).x;
	float4 sky_color = texCube_skybox(rd);

	
	if(t>0)
	{
		
		float3 Ip = LookFrom + rd * t;
		float3 color_base;
		float3 N;
		
		if(length(Ip - reactor)<radio_reactor)
		{
			float t0 = sphere(LookFrom, rd  , reactor , radio_reactor).y;
			t = t0;
			Ip = LookFrom + rd * t;
			if(dot(Ip,Ip)>r2)
				discard;
			
			color_base = float3(0.25,0.25,0.25);
			N = normalize(reactor - Ip);
		}
		else
		{
			float phi = atan2(length(Ip.xz) , Ip.y) / 6.2831;
			if(fmod(phi*3600,150)<5)
				discard;
			float theta = 0.5 + atan2(Ip.x , Ip.z) / 6.2831;
			if(fmod(theta*3600,150)<5)
				discard;
			N = normalize(Ip);
			float2 tx = float2(theta,phi)*500;
			color_base = tex2D(ds_surface, tx).rgb;
			
			float t0 = sphere(LookFrom, rd  , float3(0,0,0) , star_r+0.1);
			float d = t - t0;
			if(d>0.1)
				color_base = lerp(color_base , sky_color.rgb , clamp(d-0.1 , 0, 2) / 2);
				
		}
		
		float3 L = normalize(lightPosition.xyz - Ip);
		float k = clamp( abs(dot(N, L)) + 0.2 , 0, 1);
		rta.color = float4(color_base * k , 1);
		
		float Z = clamp(Zn + t , Zn,Zf);
		rta.depth = MatProjQ * (1-Zn / Z);
	}
	else
	{
		rta.color = sky_color;
		rta.depth = 1;
	}
	
	return rta;
}


technique DeathStar
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 ps_ds();
	}
}


/* deprecado
float udBox( float3 p, float3 b )
{
  return length(max(abs(p)-b,0.0));
}


float opRep( float3 p, float3 c )
{
    float3 q = round(fmod(p,c))-0.5*c;
    return udBox( q , float3(10,10,10));
}

// distance function
float map(float3 p) {
    //float sphere = length(p) - star_r;
	return opRep(p,float3(200,200,200));
}

// raymarching function
float trace(float3 o, float3 r) 
{
    float t = 0.;
    for (int i = 0; i < 20; ++i) {
        float d = map(o + r * t);
        t += d;
    }
    return t;
}


PS_OUTPUT ps_ds2(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) 
{
	PS_OUTPUT rta = (PS_OUTPUT)0;
	float x = vpos.x;
	float y = vpos.y;
	float3 rd = normalize(ViewDir + Dy*(0.5*(screen_dy-2*y)) - Dx*(0.5*(2*x-screen_dx)));	

	float t0= sphere(LookFrom, rd,float3(0,0,0),star_r );
	if(t0>0)
	{
		float t = trace(LookFrom+ rd*t0, rd);
		//if(t>0 && t<Zf)
		{
			float3 Ip = LookFrom + rd*t;
			float phi = atan2(length(Ip.xz) , Ip.y) / 6.2831;
			if(fmod(phi*3600,150)<5)
				discard;
			float theta = 0.5 + atan2(Ip.x , Ip.z) / 6.2831;
			if(fmod(theta*3600,150)<5)
				discard;
			
			float2 tx = float2(theta,phi)*500;
			rta.color = tex2D(ds_surface, tx);
			float Z = clamp(Zn + t , Zn,Zf);
			rta.depth = MatProjQ * (1-Zn / Z);
		}
	}
	else
	{
		rta.color = float4(0,0,0,1);
		rta.depth = 1;
	}
		
   	return rta;
}


float4 ps_skybox2(PS_INPUT input) : COLOR0
{
	float3 d = normalize(input.WorldPosition);
	float3 absd = abs(d);
	float s0 = 0; 
	float t0 = 0; 
	float s,t;
	float sc, tc, ma;


	if ((absd.x >= absd.y) && (absd.x >= absd.z)) 
	{
		if (d.x > 0.0f) 
		{
			// right
			s0 = 0.5 , t0 = 1.0/3.0;
			sc = -d.z; tc = -d.y; ma = absd.x;
		} 
		else 
		{
			// left
			s0 = 0 , t0 = 1.0/3.0;
			sc = d.z; tc = -d.y; ma = absd.x;
		}
	}
	if ((absd.y >= absd.x) && (absd.y >= absd.z)) 
	{
		if (d.y > 0.0f) 
		{
			// top
			s0 = 0.25 , t0 = 0;
			sc = d.x; tc = d.z; ma = absd.y;
		} 
		else 
		{
			// bottom
			s0 = 0.25 , t0 = 2.0/3.0;
			sc = d.x; tc = -d.z; ma = absd.y;
		}
	}
	if ((absd.z >= absd.x) && (absd.z >= absd.y)) 
	{
		if (d.z > 0.0f) 
		{
			// front
			s0 = 0.25 , t0 = 1.0/3.0;
			sc = d.x; tc = -d.y; ma = absd.z;
		} 
		else 
		{
			// back
			s0 = 0.75 , t0 = 1.0/3.0;
			sc = -d.x; tc = -d.y; ma = absd.z;
		}
	}

	if (ma == 0.0f) 
	{
		s = 0.0f;
		t = 0.0f;
	} 
	else 
	{
		s = ((sc / ma) + 1.0f) * 0.5f;
		t = ((tc / ma) + 1.0f) * 0.5f;
	}
	float ep = 0.01f;
	s = clamp(s , ep , 1-ep);
	t = clamp(t , ep , 1-ep);
	return tex2Dlod(diffuseMap, float4(s0+ s /4.0 ,t0+ t/3.0,0,0));
}


float4 ps_skybox(PS_INPUT input) : COLOR0
{
	float3 pos = normalize(input.WorldPosition);
	float u = atan2(pos.x , pos.z) / (2*PI) + 0.5;
	float v = atan2(length(pos.xz) , pos.y) / (2*PI);
	return tex2D(diffuseMap, float2(u,v));
}

technique SkyBox
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 vs_skybox();
		PixelShader = compile ps_3_0 ps_skybox2();
	}
}

PS_OUTPUT ps_ds(in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) 
{
	PS_OUTPUT rta;
	float x = vpos.x;
	float y = vpos.y;
	float3 rd = normalize(ViewDir + Dy*(0.5*(screen_dy-2*y)) - Dx*(0.5*(2*x-screen_dx)));	
	float2 q = sphere(LookFrom, rd ,float3(0,0,0) , star_r);
	float t = q.x>0 ? q.x : q.y;
	float4 sky_color = texCube_skybox(rd);
	
	if(t>0)
	{
		float3 Ip = LookFrom + rd * t;
		float3 color_base;
		float3 N;
		int lighting = 1;
		if(length(Ip - reactor)<radio_reactor)
		{
			float t0 = sphere(LookFrom, rd  , reactor , radio_reactor).y;
			t = t0;
			Ip = LookFrom + rd * t;
			if(dot(Ip,Ip)>r2)
				discard;
			
			color_base = float3(0.25,0.25,0.25);
			N = normalize(reactor - Ip);
		}
		else
		{
			if(abs(Ip.y)<100)
			{
				t = 0;
				float t1 = sphere(LookFrom, rd ,float3(0,0,0) , star_r-500).x;
				if(t1!=0 && (t1<t || t==0))
				{
					t = t1;
					Ip = LookFrom + rd * t;
					color_base.rgb = 0.1;
					lighting = 0;
				}
				
				t1 = disc(LookFrom, rd ,float3(0,100,0) , star_r);
				if(t1!=0 && (t1<t || t==0))
				{
					t = t1;
					Ip = LookFrom + rd * t;
					color_base = tex2D(ds_surface, Ip.xz*0.001).rgb;
					lighting = 0;
				}
				
				t1 = disc(LookFrom, rd ,float3(0,-100,0) , star_r);
				if(t1!=0 && (t1<t || t==0))
				{
					t = t1;
					Ip = LookFrom + rd * t;
					color_base = tex2D(ds_surface, Ip.xz*0.001).rgb;
					lighting = 0;
				}
			}
			else
			{
				float phi = atan2(length(Ip.xz) , Ip.y) / 6.2831;
				if(fmod(phi*3600,150)<5)
					discard;
				float theta = 0.5 + atan2(Ip.x , Ip.z) / 6.2831;
				if(fmod(theta*3600,150)<5)
					discard;
				N = normalize(Ip);
				float2 tx = float2(theta,phi)*200;
				color_base = tex2D(ds_surface, tx).rgb;
				
				float d = abs(q.x - q.y);
				if(d<500)
				{
					color_base = lerp(sky_color.rgb , float3(0.5,0.5,0.5) , pow(d /500 , 3));
					lighting = 0;
				}
			}
				
		}
		
		float3 L = normalize(lightPosition.xyz - Ip);
		float k = lighting ? clamp( abs(dot(N, L)) + 0.2 , 0, 1) : 1;
		rta.color = float4(color_base * k , 1);
		
		float Z = clamp(Zn + t , Zn,Zf);
		rta.depth = MatProjQ * (1-Zn / Z);
	}
	
	
	if(t<=0)
	{
		rta.color = sky_color;
		rta.depth = 1;
	}
	
	return rta;
}


*/

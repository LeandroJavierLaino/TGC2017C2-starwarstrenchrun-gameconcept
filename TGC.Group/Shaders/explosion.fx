//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))
float4x4 matProj;			// Projection

float screen_dx;					// tamaño de la pantalla en pixels
float screen_dy;
float time;


void VSCopy(float4 vPos : POSITION, float2 vTex : TEXCOORD0, out float4 oPos : POSITION, out float2 oScreenPos : TEXCOORD0)
{
	oPos = vPos;
	oScreenPos = vTex;
	oPos.w = 1;
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




// volume explosion shader
// simon green / nvidia 2012
// http://developer.download.nvidia.com/assets/gamedev/files/gdc12/GDC2012_Mastering_DirectX11_with_Unity.pdf
const float _DistThreshold = 0.005;
// parametros
float4 _Sphere = float4(0,0,0,150.0);
float _NoiseFreq = 0.1;
float _NoiseAmp = -50.0;
const float3 _NoiseAnim = float3(1, 1, 1);
float _ExploAlpha = 1;

float hash( float n )
{
    return frac(sin(n)*43758.5453);
}


// funcion que genera un numero aleatoreo
float enoise( in float3 x )
{
    float3 p = floor(x);
    float3 f = frac(x);
    f = f*f*(3.0-2.0*f);
    float n = p.x + p.y*57.0 + 113.0*p.z;
    float res = lerp(lerp(lerp( hash(n+  0.0), hash(n+  1.0),f.x),
                        lerp( hash(n+ 57.0), hash(n+ 58.0),f.x),f.y),
                    lerp(lerp( hash(n+113.0), hash(n+114.0),f.x),
                        lerp( hash(n+170.0), hash(n+171.0),f.x),f.y),f.z);
    return res;
}

// Fractional Brownian motion
//https://en.wikipedia.org/wiki/Fractional_Brownian_motion
// f es la frecuencia en un punto p, y le va generando ruidos en distintas "octavas", 
// la primer octava es 0.5, luego divide por 2, 0.25 y asi sucesivamente cada octava es 1/2 de la frecuencia anterior
// ademas tiene algunos desfasajes
float fbm( float3 p )
{
    float f;
    f = 0.5000*enoise( p );  p = p*2.02;
    f += 0.2500*enoise( p ); p = p*2.03;
    f += 0.1250*enoise( p ); p = p*2.01;
    f += 0.0625*enoise( p );
    p = p*2.02; f += 0.03125*abs(enoise( p ));	
    return f/0.9375;
}

// la explosion se simula usando distancemap
// http://www.iquilezles.org/www/articles/raymarchingdf/raymarchingdf.htm

// funcion que devuelve la distancia a una esfera. La esfera representa la explosion
float sphereDist(float3 p, float4 sphere)
{
    return length(p - sphere.xyz) - sphere.w;
}


// la funcion de distancia pp dicha, parte de la distancia a la esfera y luego le suma
// un desplazamiento que simula la explosion. Ese desplazamiento usa el fbm, y como parametro 
// entra en juego la frecuencia del ruido y la velocidad de animacion 
// displace va de 0 a 1
float distanceFunc(float3 p, out float displace)
{	
	float d = sphereDist(p , _Sphere);
	displace = fbm((p-_Sphere.xyz)*_NoiseFreq + _NoiseAnim*time);
	d += displace * _NoiseAmp;
	return d;
}


// calculate normal from distance field
float3 dfNormal(float3 pos)
{
    float eps = 0.001;
    float3 n;
    float s;
    float d = distanceFunc(pos, s);
    n.x = distanceFunc( float3(pos.x+eps, pos.y, pos.z), s ) - d;
    n.y = distanceFunc( float3(pos.x, pos.y+eps, pos.z), s ) - d;
    n.z = distanceFunc( float3(pos.x, pos.y, pos.z+eps), s ) - d;
    return normalize(n);
}

// color gradient 
// la idea es que se usa el displacement desde la esfera original, para determinar el color del punto
// lo que esta mas cerca del origen (displace ==0) se ve blanco "caliente", y a medida que se aleja 
// se va tornando negro humo ya que las particulas se enfrian

float4 gradient(float x)
{
	const float4 c0 = float4(4, 4, 4, 1);	// hot white
	const float4 c1 = float4(1, 1, 0, 1);	// yellow
	const float4 c2 = float4(1, 0, 0, 1);	// red
	const float4 c3 = float4(0.4, 0.4, 0.4,4);	// grey
	
	float t = frac(x*3.0);
	float4 c;
	if (x < 0.3333) {
		c =  lerp(c0, c1, t);
	} else if (x < 0.6666) {
		c = lerp(c1, c2, t);
	} else {
		c = lerp(c2, c3, t);
	}
	return c;
}

float4 shade(float3 p, float displace)
{	
	// lookup in color gradient
	displace = displace*1.5 - 0.2;
	displace = clamp(displace, 0.0, 0.99);
	float4 c = gradient(displace);
	// lighting (simula una fuente de luz arriba)
	float3 n = dfNormal(p);
	float diffuse = n.z*0.5+0.5;
	c.rgb = lerp(c.rgb, c.rgb*diffuse, clamp((displace-0.5)*2.0, 0.0, 1.0));
	c.a = _ExploAlpha;
	return c;
}

// sphere trace: algoritmo estandard de ray tracing distance fields
float3 sphereTrace(uniform int max_steps,float3 rayOrigin, float3 rayDir, out bool hit, out float displace)
{
	float3 pos = rayOrigin;
	hit = false;
	displace = 0.0;	
	float d;
	float disp;
	for(int i=0; i<max_steps; i++) {
		d = distanceFunc(pos, disp);
        	if (d < _DistThreshold) {
			hit = true;
			displace = disp;
        	}
		pos += rayDir*d;
	}
	
	return pos;
}


PS_OUTPUT ps_explosion(uniform int max_steps, in float2 Tex : TEXCOORD0, in float2 vpos : VPOS) 
{
	float x = vpos.x;
	float y = vpos.y;
	float3 rd = normalize(ViewDir + Dy*(0.5*(screen_dy-2*y)) - Dx*(0.5*(2*x-screen_dx)));	
    // sphere trace distance field
    bool hit;
    float displace;
    float3 hitPos = sphereTrace(max_steps,LookFrom, rd, hit, displace);
    if (!hit) 
		discard;
		
	// shade
	PS_OUTPUT rta;
	rta.color = shade(hitPos, displace);	
	float t = length(hitPos - LookFrom);
	float Z = clamp(Zn + t , Zn,Zf);
	rta.depth = MatProjQ * (1-Zn / Z);
	return rta;
}

technique Explosion
{
	pass Pass_0
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 ps_explosion(4);
	}
	pass Pass_1
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 ps_explosion(8);

	}
	pass Pass_2
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 ps_explosion(16);
	}
	pass Pass_3
	{
		VertexShader = compile vs_3_0 VSCopy();
		PixelShader = compile ps_3_0 ps_explosion(32);
	}
	
}




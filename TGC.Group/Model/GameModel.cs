using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Utils;
using System.Collections.Generic;
using TGC.Core.Shaders;
using TGC.Core.BoundingVolumes;
using TGC.Core.Collision;
using System;
using TGC.Core.Terrain;
using System.Windows.Forms;
using TgcViewer.Utils.Gui;
using TGC.Core.Sound;

namespace TGC.Group.Model
{

    static class defines
    {
        public const int MODO_CAMARA = 0;
        public const int MODO_GAME = 1;
        public const int MODO_TEST_BLOCK = 2;
        public const int MODO_INTRO = 3;
    }

    // Vertex format posicion y color
    public struct VERTEX_POS_COLOR
    {
        public float x, y, z;		// Posicion
        public int color;		// Color
    };

    // Vertex format para dibujar en 2d 
    public struct VERTEX2D
    {
        public float x, y, z, rhw;		// Posicion
        public int color;		// Color
    };


    public struct Disparo
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float timer;
        public float delay;
        public bool enemigo;
        public bool draw;
        public int block;
    };

    public struct Explosion
    {
        public Vector3 Position;
        public float timer;
        public float tiempo_explosion;
    };

    public class Block
    {
        private static readonly Random random = new Random();
        public static int tipo_o  = 0;
        public static float k = 0.11f;
        public static int r = 8;
        public static float largo = 10 * 2 * (r);
        public static float ancho = 10 * 2 * (r);
        public static float alto = 20;
        public Vector3 Position;
        public Matrix Orient;
        public Matrix MatPos;
        public Vector3 escale = new Vector3(1, 1.82f, 1) * k;
        public Matrix[] matWorld = new Matrix[500];
        public int[] mesh_index = new int[500];
        public int[] mesh_type = new int[500];
        public Vector3[] pmin = new Vector3[500];
        public Vector3[] pmax = new Vector3[500];
        public int cant_mesh;
        public TgcBoundingAxisAlignBox BoundingBox;
        public GameModel model;
        public int tipo;
        public Matrix matWorldBlock , matInvWorldBlock;
        public int cant_obstaculos = 3;
        // torreta 
        public int p_torreta = -1;           // nro de mesh que representa la torreta  (-1 si no tiene)
        public float timer_disparo = 0;


        public static Matrix OrientTecho = Helper.MatrixfromBasis(1, 0, 0,
                                                        0, -1, 0,
                                                        0, 0, 1);

        public static Matrix OrientPared = Helper.MatrixfromBasis(1, 0, 0,
                                                        0, 0, -1,
                                                        0, 1, 0);

        public static Matrix OrientParedI = Helper.MatrixfromBasis(1, 0, 0,
                                                        0, 0, 1,
                                                        0, 1, 0);

        public static Matrix OrientParedU = Helper.MatrixfromBasis(0, 1, 0,
                                                     1, 0, 0,
                                                     0, 0, 1);

        public static Matrix OrientParedD = Helper.MatrixfromBasis(0, 1, 0,
                                                     -1, 0, 0,
                                                     0, 0, 1);



        public Block(Vector3 pos, GameModel pmodel, int ptipo , Matrix OrientBlock,int pcant_obstaculos)
        {
            model = pmodel;
            tipo = ptipo;
            Position = pos;
            Orient = ptipo ==0 ? Helper.CalcularUVN(pos) : Matrix.Identity;
            MatPos = Matrix.Translation(pos);
            matWorldBlock = matInvWorldBlock = OrientBlock * Orient* MatPos;
            matInvWorldBlock.Invert();
            cant_obstaculos = pcant_obstaculos;

            switch (ptipo)
            {
                default:
                    break;

                case 0:
                    Block0(OrientBlock);
                    break;

                case 2:
                    Block2(OrientBlock);
                    break;

            }
            // calculo el bounding box de toto el bloque
            Matrix T = Orient * MatPos;
            Vector3[] p = new Vector3[8];
            float min_x = 10000000, min_y = 10000000, min_z = 10000000;
            float max_x = -10000000, max_y = -10000000, max_z = -10000000;
            p[0] = new Vector3(-largo / 2, -alto / 2, -ancho / 2);
            p[1] = new Vector3(largo / 2, -alto / 2, -ancho / 2);
            p[2] = new Vector3(largo / 2, alto / 2, -ancho / 2);
            p[3] = new Vector3(-largo / 2, alto / 2, -ancho / 2);

            p[4] = new Vector3(-largo / 2, -alto / 2, ancho / 2);
            p[5] = new Vector3(largo / 2, -alto / 2, ancho / 2);
            p[6] = new Vector3(largo / 2, alto / 2, ancho / 2);
            p[7] = new Vector3(-largo / 2, alto / 2, ancho / 2);

            for (int i = 0; i < 8; ++i)
            {
                p[i].TransformCoordinate(T);
                if (p[i].X < min_x)
                    min_x = p[i].X;
                if (p[i].Y < min_y)
                    min_y = p[i].Y;
                if (p[i].Z < min_z)
                    min_z = p[i].Z;
                if (p[i].X > max_x)
                    max_x = p[i].X;
                if (p[i].Y > max_y)
                    max_y = p[i].Y;
                if (p[i].Z > max_z)
                    max_z = p[i].Z;
            }
            BoundingBox = new TgcBoundingAxisAlignBox(new Vector3(min_x, min_y, min_z), new Vector3(max_x, max_y, max_z));


        }

        public int CreateObstaculo(Matrix OrientBlock,int t)
        {
            // obstaculos
            // nota: en el espacio del bloque 
            // X+ es hacia adelante , X- atras
            // Y+ es hacia arriba , Y- abajo
            // Z+ es hacia derecha, Z- izquierda

            Matrix S = Matrix.Scaling(k,k,k);
            Matrix O = Helper.MatrixfromBasis(0, 1, 0,
                                             -1, 0, 0,
                                              0, 0, -1);


            for (int i = 0; i < cant_obstaculos; ++i)
            {
                int q;
                if (random.Next(0, 100) < 25 && p_torreta == -1)
                {
                    // creo una torreta que dispara (solo puede haber una por block)
                    q = 6;
                    // y me guado el indice para actualizar los disparos 
                    p_torreta = t;
                }
                else
                {
                    q = tipo_o++ % 7;
                    if (random.Next(0, 100) < 25)
                        q = tipo_o++ % 7;
                    if (random.Next(0, 100) < 25)
                        q = tipo_o++ % 7;
                }

                float X = (-0.5f + 1.0f / (cant_obstaculos + 1) * (1 + i)) * largo;
                float xdesde = 0, xhasta = 0, ydesde = 0, yhasta = 0, zdesde = 0, zhasta = 0;
                xdesde = X - 2;
                xhasta = X + 2;

                switch (q)
                {
                    case 0:
                        // abajo
                        zdesde = -10; zhasta = 10; ydesde = -10; yhasta = 0.25f;
                        matWorld[t] = S * O * Matrix.Scaling(new Vector3(4.0f, 1, 2)) 
                            * Matrix.Translation(new Vector3(X, -5, 0)) * OrientBlock * Orient * MatPos;
                        mesh_index[t] = 2;
                        break;
                    case 1:
                        // arriba
                        zdesde = -10; zhasta = 10; ydesde = -0.25f; yhasta = 10;
                        matWorld[t] = S * O * Matrix.Scaling(new Vector3(4.0f, 1, 2))
                            * Matrix.Translation(new Vector3(X, 5, 0)) * OrientBlock * Orient * MatPos;
                        mesh_index[t] = 2;
                        break;
                    case 2:
                        // izquierda
                        zdesde = -10; zhasta = 0.25f; ydesde = -10; yhasta = 10;
                        matWorld[t] = S * O * Matrix.Scaling(new Vector3(4.1f, 2, 1))
                            * Matrix.Translation(new Vector3(X, 0, -5)) * OrientBlock * Orient * MatPos;
                        mesh_index[t] = 0;
                        break;

                    case 3:
                        // derecha
                        zdesde = -0.25f; zhasta = 10; ydesde = -10; yhasta = 10;
                        matWorld[t] = S * O * Matrix.Scaling(new Vector3(4.1f, 2, 1))
                            * Matrix.Translation(new Vector3(X, 0, 5)) * OrientBlock * Orient * MatPos;
                        mesh_index[t] = 0;
                        break;

                    case 4:
                        // izquierda y derecha
                        zdesde = -10; zhasta = -2.5f; ydesde = -10; yhasta = 10;
                        matWorld[t] = S * O * Matrix.Scaling(new Vector3(4.1f, 2, 0.6f)) 
                            * Matrix.Translation(new Vector3(X, 0, -6)) * OrientBlock * Orient * MatPos;
                        pmin[t] = new Vector3(xdesde, ydesde, zdesde);
                        pmax[t] = new Vector3(xhasta, yhasta, zhasta);
                        mesh_type[t] = 1;
                        mesh_index[t++] = 3;

                        zdesde = 2.5f; zhasta = 10; ydesde = -10; yhasta = 10;
                        matWorld[t] = S *O * Matrix.Scaling(new Vector3(4.1f, 2, 0.6f))
                            * Matrix.Translation(new Vector3(X, 0, 6)) * OrientBlock * Orient * MatPos;
                        mesh_index[t] = 0;
                        break;

                    case 5:
                        // arriba y abajo
                        zdesde = -10; zhasta = 10; ydesde = -10; yhasta = -2.5f;
                        matWorld[t] = S * O * Matrix.Scaling(new Vector3(4.1f, 0.6f, 2))
                            * Matrix.Translation(new Vector3(X, -6, 0)) * OrientBlock * Orient * MatPos;
                        pmin[t] = new Vector3(xdesde, ydesde, zdesde);
                        pmax[t] = new Vector3(xhasta, yhasta, zhasta);
                        mesh_type[t] = 1;
                        mesh_index[t++] = 3;

                        zdesde = -10; zhasta = 10; ydesde = 2.5f; yhasta = 10;
                        matWorld[t] = S * O * Matrix.Scaling(new Vector3(4.1f, 0.6f, 2))
                            * Matrix.Translation(new Vector3(X, 6, 0)) * OrientBlock * Orient * MatPos;
                        mesh_index[t] = 0;
                        break;

                    case 6:
                        // torreta abajo
                        zdesde = -3; zhasta = 3; ydesde = -10; yhasta = 0.25f;
                        matWorld[t] = S * Matrix.Scaling(new Vector3(0.7f, 0.7f, 0.7f)) * Matrix.RotationY(FastMath.PI)
                            * Matrix.Translation(new Vector3(X,-5,0)) * OrientBlock * Orient * MatPos;
                        mesh_index[t] = 5;
                        break;



                }

                // almaceno el bounding box del obstaculo
                pmin[t] = new Vector3(xdesde, ydesde, zdesde);
                pmax[t] = new Vector3(xhasta, yhasta, zhasta);
                mesh_type[t] = 1;
                ++t;

                // los bloques con torreta, no tienen ninguna otra cosa mas
                if (i == 6)
                    break;
            }

            return t;
        }

        public void Block0(Matrix OrientBlock)
        {
            int prof = 2;       // profundidad del trench
            // piso y techo
            int t = 0;
            for (int i = -r; i < r; ++i)
            {
                //  piso
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), -5)) * OrientBlock * Orient * MatPos;
                ++t;


                // pared
                for (int s = 0; s < prof; ++s)
                {
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, -10)) * OrientBlock * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, 10)) * OrientBlock * Orient * MatPos;
                }
            }

            for (int i = 0; i < t; ++i)
            {
                mesh_type[t] = 0;           // paredes, techos, etc
                mesh_index[i] = random.Next(0, 5);
            }

            cant_mesh = CreateObstaculo(OrientBlock, t);

        }



        public void Block2(Matrix OrientBlock)
        {
            int prof = 2;       // profundidad del trench
            // piso y techo
            int t = 0;
            for (int i = -r; i < r; ++i)
            {
                //  piso
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), 5)) * OrientBlock * Orient * MatPos;
                ++t;
                matWorld[t] = Matrix.Scaling(escale) * Matrix.Translation(new Vector3(i * 10, -10 * (prof - 1), -5)) * OrientBlock * Orient * MatPos;
                ++t;
                // pared
                for (int s = 0; s < prof; ++s)
                {
                    matWorld[t++] = Matrix.Scaling(escale) * OrientParedI * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, -10)) * OrientBlock * Orient * MatPos;
                    matWorld[t++] = Matrix.Scaling(escale) * OrientPared * Matrix.Translation(new Vector3(i * 10, 5 - 10 * s, 10)) * OrientBlock * Orient * MatPos;
                }
            }


            for (int i = 0; i < t; ++i)
            {
                mesh_type[t] = 0;           // paredes, techos, etc
                mesh_index[i] = random.Next(0, 5);
            }

            cant_mesh = CreateObstaculo(OrientBlock, t);
        }

        public bool render()
        {
            float dist = (Position - model.Camara.Position).LengthSq();

            if (model.curr_mode == defines.MODO_GAME)
            {
                float dist_lod = 500000;
                if (dist > dist_lod)
                    return false;
            }


            // trench 
            for (int i = 0; i < cant_mesh; ++i)
            {
                int index = mesh_index[i];
                if (index != -1 )
                {
                    bool torreta = index == 5 || index == 12 ? true : false;
                    if (dist>20000 && index <= 5)
                        index += 7;
                    model.meshes[index].Transform = matWorld[i];
                    TgcShaders.Instance.setShaderMatrix(model.effect, matWorld[i]);
                    model.effect.SetValue("ssao", torreta ?0:model.ssao?1:0);
                    model.effect.SetValue("f_red", torreta ? 0.15f : 0);        // la torreta unpoco roja
                    model.effect.CommitChanges();
                    // caso particular: la torreta tiene todo en el layer 25 y no tengo el sketchup para corregirlo
                    model.meshes[index].D3dMesh.DrawSubset(torreta ? 25 : 0);
                }
            }
            return true;
        }

        public void Update()
        {
            // actualizo solo el current block y los n siguientes (que como va al reves, son los anteriores)
            if (p_torreta == -1)
                return;     // no tiene lo que hacer de momento

            // verifico el timer de la torreta (si es cero, hago un disparo)
            if(timer_disparo==0)
            {
                int p = model.p_disparo++;
                if (model.p_disparo >= model.disparos.Length)
                    model.p_disparo = 0;

                model.disparos[p].enemigo = true;
                model.disparos[p].timer = 0.5f;
                model.disparos[p].delay = 0.0f;
                Vector3 pt = new Vector3(0, 45, 0);
                pt.TransformCoordinate(matWorld[p_torreta]);
                model.disparos[p].Position = pt;
                Vector3 d = new Vector3(20, 1, 0);
                d.TransformNormal(matWorld[p_torreta]);
                d.Normalize();
                model.disparos[p].Velocity = d;
                timer_disparo = 0.3f;
            }
            else
            {
                timer_disparo -= model.ElapsedTime;
                if (timer_disparo < 0)
                    timer_disparo = 0;
            }
        }

        public bool colisiona(Vector3 pt)
        {
            // transformo el punto al espacio del block
            pt.TransformCoordinate(matInvWorldBlock);

            // choca contra las paredes
            if (pt.Z > 10 || pt.Z < -10 || pt.Y<-10)
                return true;

            // nota: en el espacio del bloque 
            // X+ es hacia adelante , X- atras
            // Y+ es hacia arriba , Y- abajo
            // Z+ es hacia derecha, Z- izquierda

            bool rta = false;
            for (int i = 0; i < cant_mesh && !rta; ++i)
            {
                if(mesh_type[i]==1)
                {
                    // obstaculo
                    if (pt.Z >= pmin[i].Z && pt.Z <= pmax[i].Z && 
                        pt.Y >= pmin[i].Y && pt.Y <= pmax[i].Y &&
                        pt.X >= pmin[i].X && pt.X <= pmax[i].X)
                        rta = true;
                }
            }
            return rta;
        }




    }


    /// <summary>
    ///     Ejemplo para implementar el TP.
    ///     Inicialmente puede ser renombrado o copiado para hacer más ejemplos chicos, en el caso de copiar para que se
    ///     ejecute el nuevo ejemplo deben cambiar el modelo que instancia GameForm <see cref="Form.GameForm.InitGraphics()" />
    ///     line 97.
    /// </summary>
    public class GameModel : TgcExample
    {
        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;
        }


        //public int curr_mode = defines.MODO_CAMARA;
        //public int curr_mode = defines.MODO_TEST_BLOCK;
        public int curr_mode = defines.MODO_GAME;
        public bool camara_ready = false;

        public TgcBox Box { get; set; }
        public List<TgcMesh> meshes = new List<TgcMesh>();
        public List<TgcMesh> xwing = new List<TgcMesh>();
        private XwingShips xwingShips;
        public TgcArrow ship = new TgcArrow();
        public TgcBox BlockSurface;
        public TgcBox BlockTrench;
        public TgcBox LODMesh;

        public int vidas = 3;
        public float star_r = 8000;
        public Vector3 ship_k = new Vector3(0.09f , 0.09f , 0.11f);
        public Vector3 ship_vel;
        public Vector3 ship_N;
        public Vector3 ship_bitan;
        public float ship_speed;
        public Vector3 ship_pos;
        public float ship_an = 0;
        public float ship_an_base = 0;
        public float ship_anV = 0;
        public float ship_H;
        public Vector3 cam_vel = new Vector3(0, 0, 0);
        public Vector3 cam_pos = new Vector3(1, 0, 0);
        public Vector3 target_pos = new Vector3(1, 0, 0);
        public List<Block> scene = new List<Block>();
        public List<Block> trench = new List<Block>();
        public int curr_block;
        public Vector3[] collision_pt = new Vector3[5];
        public int cant_cpt = 5;        // cantidad de puntos de colision
        public int cd_index = 0;        // colision detectada
        public float explosion_timer = 0;      // timer colision detectada
        public float tiempo_explosion = 2;      // tiempo total que tarda en explotar
        public float intro_timer = 10;   // timer de introduccion
        public float r_timer = 0;      // timer de resurreccion
        public Vector4 _Sphere;

        private static readonly Random random = new Random();

        public Effect effect , effectBase , effectExplosion;
        public Surface g_pDepthStencil; // Depth-stencil buffer
        public Texture g_pRenderTarget, g_pPosition, g_pNormal;
        public Texture g_pRenderTarget4, g_pRenderTarget4Aux;


        // Shadow map
        public int SHADOWMAP_SIZE = 1024;
        public Texture g_pShadowMap;    // Texture to which the shadow map is rendered
        public Surface g_pDSShadow;     // Depth-stencil buffer for rendering to shadow map
        public Texture g_pLightMap;     // Textured light
        public Matrix g_mShadowProj;    // Projection matrix for shadow map
        Matrix g_LightView;				// matriz de view del light


        public VertexBuffer g_pVBV3D;
        public Texture textura_bloques;
        public Vector3 LightPos;
        public float time = 0;
        public int screen_dx, screen_dy;
        public bool pausa = false;

        // opciones dx
        public bool shadow_map = true;
        public bool ssao = true;
        public bool glow = true;


        public bool mouseCaptured;
        public Point mouseCenter;
        public float xm, ym;     // pos del mouse
        public float wm;
        public int eventoInterno = 0;

        // disparos
        public Disparo[] disparos = new Disparo[255];
        public int p_disparo = 0;
        // explosiones
        public Explosion[] explosiones = new Explosion[4];
        public int p_explosion = 0;

        // interface 2d
        public Sprite sprite;
        public Microsoft.DirectX.Direct3D.Font font;
        public Texture[] gui_texture = new Texture[16];

        // gui 
        DXGui gui = new DXGui();
        public bool gui_mode = true;
        public bool opciones_dx = false;

        //Sound & Music
        private Core.Sound.TgcStaticSound blaster;
        private Core.Sound.TgcStaticSound tieFighter;
        private Core.Sound.TgcMp3Player musicTrack;
        private bool playMusic = false;
        private float timerBlast = 0;


        public override void Init()
        {
            blaster = new Core.Sound.TgcStaticSound();
            blaster.loadSound(MediaDir + "blaster.wav", DirectSound.DsDevice);

            tieFighter = new Core.Sound.TgcStaticSound();
            tieFighter.loadSound(MediaDir + "TIEFighter.wav", DirectSound.DsDevice);

            musicTrack = new Core.Sound.TgcMp3Player();

            // cargo el shader
            InitDefferedShading();
            // la iluminacion
            InitLighting();
            // cargo la escena
            InitScene();
            // pos. la camara
            InitCamara();
            // el input del mouse
            // para capturar el mouse
            var focusWindows = D3DDevice.Instance.Device.CreationParameters.FocusWindow;
            mouseCenter = focusWindows.PointToScreen(new Point(focusWindows.Width / 2, focusWindows.Height / 2));
            mouseCaptured = false;
            Cursor.Position = mouseCenter;
            //            Cursor.Hide();

            xwingShips = new XwingShips(MediaDir + "X-Wing-TgcScene.xml", scene[scene.Count - 1].Position);

            xm = Input.Xpos;
            ym = Input.Ypos;
            wm = Input.WheelPos;

            // gui
            InitGui();
        }

        public void InitGui()
        {
            // gui
            DXGui.mediaDir = MediaDir;
            gui.Input = Input;
            gui.Create();
            // menu principal
            gui.InitDialog();

            int W = screen_dx;
            int H = screen_dy;
            int margen_x = 50;
            int dx = (W - 3*margen_x)/2;
            int x0 = margen_x;
            int y0 = 100;
            int dy = 20;
            int dy2 = 30;

            gui.InsertFrame("NIVEL DE DIFICULTAD", x0, y0-40, dx, 460, Color.FromArgb(200,0,0,0),
                frameBorder.redondeado);

            int pos_x = x0 + 50;
            gui_radio_button pbutton = gui.InsertRadioButton(100, "FACIL", pos_x, y0 += dy2, dx, dy);
            pbutton.marcado = true;
            gui.InsertRadioButton(101, "INTERMEDIO", pos_x, y0 += dy2, dx, dy);
            gui.InsertRadioButton(102, "DIFICIL", pos_x, y0 += dy2, dx, dy);
            gui.InsertRadioButton(103, "IMPOSIBLE", pos_x, y0 += dy2, dx, dy);
            gui.InsertButton(104, "Play", x0 + dx/2 - 100, y0 += 80, 200, 40);
            gui.InsertLine(x0, y0 += 80, dx, 0);
            gui.InsertStatic("El objetivo es avanzar en el Trench lo maximo posible", pos_x, y0 += dy2, dx, dy);
            gui.InsertStatic("esquivando obstaculos y disparos de las torretas", pos_x, y0 += dy2, dx, dy);

            y0 = 100;
            x0 = W/2+margen_x/2;
            gui.InsertFrame("COMANDOS", x0, y0 - 40, dx, 460, Color.FromArgb(200, 0, 0, 0),frameBorder.redondeado);
            pos_x = x0 + 50;
            gui.InsertImagen("xwing.png", pos_x, y0, 0, dy);
            gui.InsertStatic("mover la nave", x0, y0 += dy2, dx/2, dy,DrawTextFormat.Right);
            gui.InsertStatic("girar 90 grados", x0, y0 += dy2, dx/2, dy, DrawTextFormat.Right);
            gui.InsertStatic("disparar", x0, y0 += dy2, dx / 2, dy, DrawTextFormat.Right);
            gui.InsertStatic("lock mouse", x0, y0 += dy2, dx / 2, dy, DrawTextFormat.Right);
            gui.InsertStatic("pausa", x0, y0 += dy2, dx / 2, dy, DrawTextFormat.Right);
            gui.InsertStatic("opciones", x0, y0 += dy2, dx / 2, dy, DrawTextFormat.Right);

            gui.InsertLine(x0, y0 += 100, dx, 0);
            gui.InsertStatic("Prueba de Concepto de TGC Framework", pos_x, y0 += dy2, dx, dy);
            gui.InsertStatic("UTN TGC", x0, y0 += dy2, dx, dy,DrawTextFormat.Center);


            pos_x = x0 + dx/2 + 20;
            y0 = 100;
            gui.InsertImagen("xwing.png", pos_x, y0 += dy2, 0, dy);
            gui.InsertImagen("xwing.png", pos_x, y0 += dy2, 0, dy);
            gui.InsertImagen("xwing.png", pos_x, y0 += dy2, 0, dy);
            gui.InsertImagen("xwing.png", pos_x, y0 += dy2, 0, dy);
            gui.InsertImagen("xwing.png", pos_x, y0 += dy2, 0, dy);


            pos_x = x0 + dx / 2 + 50;
            y0 = 100;
            gui.InsertStatic("[MOUSE]", pos_x, y0 += dy2, dx / 2, dy);
            gui.InsertStatic("[CONTROL]", pos_x, y0 += dy2, dx / 2, dy);
            gui.InsertStatic("[A]", pos_x, y0 += dy2, dx / 2, dy);
            gui.InsertStatic("[M]", pos_x, y0 += dy2, dx / 2, dy);
            gui.InsertStatic("[P]", pos_x, y0 += dy2, dx / 2, dy);
            gui.InsertStatic("[F2]", pos_x, y0 += dy2, dx / 2, dy);


        }


        public void InitGuiOpciones()
        {
            // gui
            gui.InitDialog();
            int W = screen_dx;
            int H = screen_dy;
            int margen_x = 50;
            int dx = (W - 3 * margen_x) / 2;
            int x0 = margen_x;
            int y0 = 100;
            int dy = 20;
            int dy2 = 30;

            gui.InsertFrame("Opciones DirectX", x0, y0 - 40, dx, 460, Color.FromArgb(100, 100, 100, 0),
                frameBorder.redondeado);

            int pos_x = x0 + 50;
            gui_check_button pbutton = gui.InsertCheckButton(100, "SSAO - Screen Space Ambient Occlusion", pos_x, y0 += dy2, dx, dy);
            pbutton.marcado = true;
            pbutton = gui.InsertCheckButton(101, "Shadow Map", pos_x, y0 += dy2, dx, dy);
            pbutton.marcado = true;
            pbutton = gui.InsertCheckButton(102, "Glowing", pos_x, y0 += dy2, dx, dy);
            pbutton.marcado = true;
            gui.InsertButton(99, "Ocultar menu de opciones ", x0 + dx / 2 - 150, y0 += 80, 300, 40);
            gui.InsertButton(98, "Pausar / Continuar juego", x0 + dx / 2 - 150, y0 += 80, 300, 40);
        }



        public void InitScene()
        {
            //Device de DirectX para crear primitivas.
            var d3dDevice = D3DDevice.Instance.Device;
            var textura_surface = TgcTexture.createTexture(MediaDir + "Textures\\ds_surface.png");
            effect.SetValue("texDeathStarSurface", textura_surface.D3dTexture);
            Box = TgcBox.fromSize(new Vector3(1, 1, 1), textura_surface);
            Box.Position = new Vector3(0, 0, 0);
            Box.Effect = effect;
            Box.Technique = "DefaultTechnique";
            BlockSurface = TgcBox.fromSize(new Vector3(1, 1, 1), textura_surface);
            BlockSurface.Effect = effect;
            BlockSurface.Technique = "DefaultTechnique";
            BlockTrench = TgcBox.fromSize(new Vector3(1, 1, 1), TgcTexture.createTexture(MediaDir + "Textures\\ds_trench.png"));
            BlockTrench.Effect = effect;
            BlockTrench.Technique = "DefaultTechnique";

            var loader = new TgcSceneLoader();
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m1-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m2-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m3-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m4-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m5-TgcScene.xml").Meshes[0]);
            meshes.Add(loader.loadSceneFromFile(MediaDir + "torreta2-TgcScene.xml").Meshes[0]);       // 5
            meshes.Add(loader.loadSceneFromFile(MediaDir + "m3-TgcScene.xml").Meshes[0]);
            //meshes.Add(TgcBox.fromSize(new Vector3(100, 100, 100), textura_surface).toMesh("q2"));
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x1-TgcScene.xml").Meshes[0]);               // 7
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x2-TgcScene.xml").Meshes[0]);               // 8
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x3-TgcScene.xml").Meshes[0]);               // 9
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x4-TgcScene.xml").Meshes[0]);               // 10
            meshes.Add(loader.loadSceneFromFile(MediaDir + "x5-TgcScene.xml").Meshes[0]);               // 11
            meshes.Add(loader.loadSceneFromFile(MediaDir + "torreta2-TgcScene.xml").Meshes[0]);          // 12

            LODMesh = TgcBox.fromSize(meshes[0].BoundingBox.calculateSize(),
                        TgcTexture.createTexture(MediaDir + "Textures\\m1.jpg"));
            LODMesh.Effect = effect;
            LODMesh.Technique = "DefaultTechnique";

            foreach (TgcMesh mesh in meshes)
            {
                mesh.Scale = new Vector3(1f, 1f, 1f);
                mesh.AutoTransformEnable = false;
                mesh.Effect = effect;
                mesh.Technique = "DefaultTechnique";
            }

            xwing = loader.loadSceneFromFile(MediaDir + "xwing-TgcScene.xml").Meshes;
            foreach (TgcMesh mesh in xwing)
            {
                mesh.AutoTransformEnable = false;
                mesh.Effect = effect;
                mesh.Technique = "DefaultTechnique";
            }

            if (curr_mode == defines.MODO_TEST_BLOCK)
            {
                scene.Add(new Block(new Vector3(0,0,0), this, 2, Matrix.Identity,1));
            }
            else
            {
                ArmarEcuatorialTrench();
            }



            var textura_skybox = TgcTexture.createTexture(MediaDir + "Textures\\Color A05.png");
            effect.SetValue("texSkybox", textura_skybox.D3dTexture);
            textura_bloques = TgcTexture.createTexture(MediaDir + "Textures\\4.png").D3dTexture;

            // puntos de colision de la nave
            collision_pt[0] = new Vector3(0, -10, -50);          // ala izquierda
            collision_pt[1] = new Vector3(0, 10, -50);          // ala izquierda
            collision_pt[2] = new Vector3(0, -10, 50);            // ala derecha
            collision_pt[3] = new Vector3(0, 10, 50);            // ala derecha
            collision_pt[4] = new Vector3(50, 0, 0);          // frente

            // sistema de disparos
            for (int i = 0; i < disparos.Length; ++i)
                disparos[i].timer = 0;
            // sistema de explosiones
            for (int i = 0; i < explosiones.Length; ++i)
                explosiones[i].timer = 0;

            sprite = new Sprite(d3dDevice);
            // Fonts
            font = new Microsoft.DirectX.Direct3D.Font(d3dDevice, 24, 0, FontWeight.Light, 0, false, CharacterSet.Default,
                    Precision.Default, FontQuality.Default, PitchAndFamily.DefaultPitch, "Lucida Console");
            font.PreloadGlyphs('0', '9');
            font.PreloadGlyphs('a', 'z');
            font.PreloadGlyphs('A', 'Z');

            gui_texture[0] = TgcTexture.createTexture(MediaDir + "gui\\scoreboard.png").D3dTexture;
            gui_texture[1] = TgcTexture.createTexture(MediaDir + "gui\\fondo.png").D3dTexture;

        }

        public void InitCamara()
        {
            Vector3 cameraPosition;
            Vector3 lookAt;
            if (curr_mode == defines.MODO_GAME)
            {
                //ship_pos = new Vector3(0, 50, star_r-15);
                ship_pos = scene[scene.Count-1].Position;
                ship_vel = new Vector3(0, -1, 0);
                ship_N = new Vector3(0, 0, 1);
                ship_bitan = new Vector3(-1, 0, 0);
                ship_speed = 175;            // despues de la intro la subo a 250
            }
            else
            {
                if (curr_mode == defines.MODO_TEST_BLOCK)
                {
                    Vector3 pmin = new Vector3(10000, 10000, 10000);
                    Vector3 pmax = new Vector3(-10000, -10000, -10000);
                    foreach (Block bloque in scene)
                    {
                        if (bloque.BoundingBox.PMin.X < pmin.X)
                            pmin.X = bloque.BoundingBox.PMin.X;
                        if (bloque.BoundingBox.PMin.Y < pmin.Y)
                            pmin.Y = bloque.BoundingBox.PMin.Y;
                        if (bloque.BoundingBox.PMin.Z < pmin.Z)
                            pmin.Z = bloque.BoundingBox.PMin.Z;

                        if (bloque.BoundingBox.PMax.X > pmax.X)
                            pmax.X = bloque.BoundingBox.PMax.X;
                        if (bloque.BoundingBox.PMax.Y > pmax.Y)
                            pmax.Y = bloque.BoundingBox.PMax.Y;
                        if (bloque.BoundingBox.PMax.Z > pmax.Z)
                            pmax.Z = bloque.BoundingBox.PMax.Z;
                    }

                    lookAt = (pmin + pmax)*0.5f;
                    cameraPosition = lookAt + new Vector3(1000,500,0);
                }
                else
                {
                    cameraPosition = new Vector3(0,100, 10);
                    lookAt = new Vector3(0, 0, 0);
                }
                Camara.SetCamera(cameraPosition, lookAt , new Vector3(0,1,0));
            }

        }



        public void InitDefferedShading()
        {
            var d3dDevice = D3DDevice.Instance.Device;
            //Cargar Shader personalizado
            string compilationErrors;
            effectBase = Effect.FromFile(d3dDevice, ShadersDir + "base.fxo", null, null, ShaderFlags.PreferFlowControl,
                null, out compilationErrors);
            if (effectBase == null)
            {
                throw new Exception("Error al cargar shader effectBase. Errores: " + compilationErrors);
            }
            effectBase.Technique = "DefaultTechnique";

            effectExplosion = Effect.FromFile(d3dDevice, ShadersDir + "explosion.fxo", null, null, ShaderFlags.PreferFlowControl,
                null, out compilationErrors);
            if (effectExplosion == null)
            {
                throw new Exception("Error al cargar shader effectExplosion. Errores: " + compilationErrors);
            }
            effectExplosion.Technique = "Explosion";

            effect = effectBase;

            g_pDepthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth,
                d3dDevice.PresentationParameters.BackBufferHeight,
                DepthFormat.D24S8, MultiSampleType.None, 0, true);

            // inicializo el render target
            g_pRenderTarget = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.X8R8G8B8,
                Pool.Default);
            // geometry buffer
            g_pNormal = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.A32B32G32R32F,
                Pool.Default);
            // geometry buffer
            g_pPosition = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget, Format.A32B32G32R32F,
                Pool.Default);

            // glow map effect
            g_pRenderTarget4 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 4
                    , d3dDevice.PresentationParameters.BackBufferHeight / 4, 1, Usage.RenderTarget,
                        Format.X8R8G8B8, Pool.Default);

            g_pRenderTarget4Aux = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth / 4
                    , d3dDevice.PresentationParameters.BackBufferHeight / 4, 1, Usage.RenderTarget,
                        Format.X8R8G8B8, Pool.Default);


            effectBase.SetValue("g_RenderTarget", g_pRenderTarget);
            // Resolucion de pantalla
            screen_dx = d3dDevice.PresentationParameters.BackBufferWidth;
            screen_dy = d3dDevice.PresentationParameters.BackBufferHeight;
            effectBase.SetValue("screen_dx", screen_dx);
            effectBase.SetValue("screen_dy", screen_dy);
            effectExplosion.SetValue("screen_dx", screen_dx);
            effectExplosion.SetValue("screen_dy", screen_dy);

            CustomVertex.PositionTextured[] vertices =
            {
            new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
            new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
            new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
            new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
             };
            //vertex buffer de los triangulos
            g_pVBV3D = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                4, d3dDevice, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            g_pVBV3D.SetData(vertices, 0, LockFlags.None);


            var textura_ruido = TgcTexture.createTexture(MediaDir + "Textures\\noise.png");
            effectBase.SetValue("texNoise", textura_ruido.D3dTexture);


            //--------------------------------------------------------------------------------------
            // Creo el shadowmap. 
            // Format.R32F
            // Format.X8R8G8B8
            g_pShadowMap = new Texture(d3dDevice, SHADOWMAP_SIZE, SHADOWMAP_SIZE,
                                        1, Usage.RenderTarget, Format.R32F,
                                        Pool.Default);

            // tengo que crear un stencilbuffer para el shadowmap manualmente
            // para asegurarme que tenga la el mismo tamaño que el shadowmap, y que no tenga 
            // multisample, etc etc.
            g_pDSShadow = d3dDevice.CreateDepthStencilSurface(SHADOWMAP_SIZE,
                                                             SHADOWMAP_SIZE,
                                                             DepthFormat.D24S8,
                                                             MultiSampleType.None,
                                                             0,
                                                             true);
            // por ultimo necesito una matriz de proyeccion para el shadowmap, ya 
            // que voy a dibujar desde el pto de vista de la luz.
            // El angulo tiene que ser mayor a 45 para que la sombra no falle en los extremos del cono de luz
            // de hecho, un valor mayor a 90 todavia es mejor, porque hasta con 90 grados es muy dificil
            // lograr que los objetos del borde generen sombras
            float aspectRatio = (float)d3dDevice.PresentationParameters.BackBufferWidth / (float)d3dDevice.PresentationParameters.BackBufferHeight;
            g_mShadowProj = Matrix.PerspectiveFovLH(Geometry.DegreeToRadian(60),aspectRatio, 1, 50000);



        }

        public void InitLighting()
        {
            Vector3 dir = new Vector3(0, -600, 300);
            dir.Normalize();
            LightPos = curr_mode == defines.MODO_TEST_BLOCK ? new Vector3(0, 150, 0) : dir * 10000;
            Vector3 LightDir = LightPos;
            LightDir.Normalize();
            //Cargar variables shader de la luz
            effectBase.SetValue("lightColor", ColorValue.FromColor(Color.FromArgb(240, 240, 255)));
            effectBase.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(LightPos));
            effectBase.SetValue("lightDir", TgcParserUtils.vector3ToFloat4Array(LightDir));
            effectBase.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
            effectBase.SetValue("lightIntensity", (float)1);
            effectBase.SetValue("lightAttenuation", (float)0);

            //Cargar variables de shader de Material. El Material en realidad deberia ser propio de cada mesh. Pero en este ejemplo se simplifica con uno comun para todos
            effectBase.SetValue("materialEmissiveColor", ColorValue.FromColor(Color.FromArgb(0, 0, 0)));
            effectBase.SetValue("materialAmbientColor", ColorValue.FromColor(Color.FromArgb(120, 120, 120)));
            effectBase.SetValue("materialDiffuseColor", ColorValue.FromColor(Color.FromArgb(120, 120, 120)));
            effectBase.SetValue("materialSpecularColor", ColorValue.FromColor(Color.FromArgb(240, 204, 155)));
            effectBase.SetValue("materialSpecularExp", (float)40);
            effectBase.SetValue("specularFactor", (float)1.3);


            
        }


        public void ArmarEcuatorialTrench()
        {
            float alfa_0 = 0;
            float alfa_1 = (float)(2*Math.PI);
            float cant_i = (int)(star_r * (alfa_1 - alfa_0) / (Block.largo * 0.8f));
            for (int i = 0; i < cant_i; ++i)
            {
                float ti = i / cant_i;
                float alfa = alfa_0 * (1 - ti) + alfa_1 * ti;
                float x = FastMath.Cos(alfa);
                float y = FastMath.Sin(alfa);
                float j = cant_i - i-1;
                scene.Add(new Block(new Vector3(0, y, x) * (star_r-10), this, 0 , Matrix.Identity, 
                         j<5? 0 :  1 + (int)Math.Floor(j/cant_i * 5)));
            }
        }




        public override void Update()
        {
            PreUpdate();
            timerBlast += ElapsedTime;
            if (!gui_mode)
            {
                if (FastMath.Abs(timerBlast) % 5 == 0) blaster.play(false);
                if (FastMath.Abs(timerBlast) % 4 == 0 || FastMath.Abs(timerBlast) % 7 == 0) tieFighter.play(false);
            }

            PreUpdate();
            if (ElapsedTime < 0 || ElapsedTime > 10)
                return;

            if (gui_mode)
            {
                GuiMessage msg = gui.Update(ElapsedTime);
                if (msg.message == MessageType.WM_COMMAND && msg.id == 104)
                {
                    gui_mode = false;
                    // de paso creo el gui de opciones
                    InitGuiOpciones();
                }
                return;
            }


            if (opciones_dx)
            {
                GuiMessage msg = gui.Update(ElapsedTime);
                if (msg.message == MessageType.WM_COMMAND)
                {
                    switch (msg.id)
                    {
                        case 99:
                            opciones_dx = false;
                            break;
                        case 98:
                            pausa = !pausa;
                            break;
                        case 100:
                            ssao = !ssao;
                            gui.GetDlgItem(100).marcado = ssao;
                            break;
                        case 101:
                            shadow_map = !shadow_map;
                            gui.GetDlgItem(101).marcado = shadow_map;
                            break;
                        case 102:
                            glow = !glow;
                            gui.GetDlgItem(102).marcado = glow;
                            break;
                    }

                }
            } 
            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.F2))
                opciones_dx = !opciones_dx;
            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.P))
                pausa = !pausa;

            if (pausa)
                return;

            // actualizo los blockes
            if (curr_mode == defines.MODO_GAME && curr_block>2)
            {
                scene[curr_block - 1].Update();                
            }
            // los disparos
            UpdateDisparos();

            // los timers
            time += ElapsedTime;
            if (intro_timer > 0)
            {
                ship_speed = 50;
                intro_timer -= ElapsedTime;
                if (intro_timer < 0)
                {
                    intro_timer = 0;
                    ship_speed = 175;
                }
            }

            if (explosion_timer>0)
            {
                ship_speed = 5;
                explosion_timer -= ElapsedTime;
                if (explosion_timer < 0)
                {
                    explosion_timer = 0;
                    ship_speed = 175;
                    // crash
                    ship_pos = scene[curr_block].Position;
                    ship_vel = new Vector3(0, -1, 0);
                    ship_N = new Vector3(0, 0, 1);
                    ship_bitan = new Vector3(-1, 0, 0);
                    ship_an_base = ship_an = 0;
                    ship_anV = 0;
                    // le doy 2 segundos de changui
                    r_timer = 2;
                }
            }

            if (r_timer > 0)
            {
                r_timer -= ElapsedTime;
                if (r_timer < 0)
                    r_timer = 0;
            }


            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.Space) && intro_timer != 0)
                intro_timer = 0.0001f;

            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.A))
            {
                blaster.play(false);
                Matrix O = Helper.MatrixfromBasis(
                                                    1, 0, 0,
                                                    0, 0, 1,
                                                    0, 1, 0
                                                    );
                if (Math.Abs(ship_an + ship_an_base) > 0.001f)
                    O = O * Matrix.RotationX(/*ship_an_base +*/ ship_an * 0.25f);
                if (Math.Abs(ship_anV) > 0.001f)
                    O = O * Matrix.RotationY(ship_anV * 0.25f);
                Matrix T = O * Helper.CalcularMatriz(ship_pos, ship_k, ship_vel, ship_bitan, ship_N);
                Vector3[] p = new Vector3[cant_cpt];
                for (int s = 0; s < cant_cpt; ++s)
                {
                    p[s] = new Vector3(0, 0, 0);
                    p[s].TransformCoordinate(Matrix.Translation(collision_pt[s]) * T);

                    disparos[p_disparo].timer = 0.5f;
                    disparos[p_disparo].delay = 0.05f * s;
                    disparos[p_disparo].Position = p[s];
                    disparos[p_disparo].Velocity = ship_vel;
                    disparos[p_disparo].enemigo = false;
                    p_disparo++;
                    if (p_disparo >= disparos.Length)
                        p_disparo = 0;

                }
            }


            if (Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftControl))
            {
                ship_an_base += ElapsedTime * 10.0f;
                if (ship_an_base > (float)Math.PI / 2.0f)
                    ship_an_base = (float)Math.PI / 2.0f;
            }
            else
            {
                ship_an_base -= ElapsedTime * 10.0f;
                if(ship_an_base<0)
                    ship_an_base = 0;
            }

            if (Input.keyPressed(Microsoft.DirectX.DirectInput.Key.M))
            {
                mouseCaptured = !mouseCaptured;
                if (mouseCaptured)
                    Cursor.Hide();
                else
                    Cursor.Show();
            }

            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_RIGHT))
                ElapsedTime = 0;

            if (Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_LEFT) || Input.buttonUp(TgcD3dInput.MouseButtons.BUTTON_MIDDLE))
            {
                eventoInterno = 0;
            }

            if (mouseCaptured || Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                if (eventoInterno == 0)
                {
                    xm = Input.Xpos * 0.1f;
                    ym = Input.Ypos * 0.1f;
                    eventoInterno = 1;
                }
                else
                {
                    float dx = Input.XposRelative;// * 0.25f;
                    float dy = Input.YposRelative;// * 0.25f;

                    if (curr_mode == defines.MODO_GAME)
                    {
                        if (!Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift))
                        {
                            dx *= 0.3f;
                            dy *= 0.5f;
                        }

                       
                        Matrix rotN = Matrix.RotationAxis(ship_N, ElapsedTime * dx);
                        ship_vel.TransformNormal(rotN);
                        ship_bitan.TransformNormal(rotN);
                        if (!Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift)) dx *= 0.3f;
                        ship_an += ElapsedTime * dx * 0.15f; 
                        ship_an = FastMath.Clamp(ship_an, -1, 1);
                        if (!Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift)) dy *= 0.5f;
                        ship_anV += ElapsedTime * dy * 0.00015f; 
                        ship_anV = FastMath.Clamp(ship_anV, -1, 1);

                        Matrix rotBT = Matrix.RotationAxis(ship_bitan, -ElapsedTime * dy);
                        ship_vel.TransformNormal(rotBT);
                        ship_N.TransformNormal(rotBT);

                    }
                    else
                    {
                        // uso el desplazamiento en x para rotar el punto de vista 
                        // en el plano xy
                        float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 0.05f : 0.5f;
                        float tot_x = 800;
                        float an = dx / tot_x * 2 * FastMath.PI * k;
                        Matrix T = Matrix.Translation(-Camara.LookAt) * Matrix.RotationY(an) * Matrix.Translation(Camara.LookAt);
                        Vector3 LF = Camara.Position;
                        LF.TransformCoordinate(T);

                        Vector3 ViewDir = Camara.LookAt - Camara.Position;
                        ViewDir.Normalize();

                        Vector3 N;
                        N = Vector3.Cross(new Vector3(0, 1, 0), ViewDir);

                        float tot_y = 600;
                        float an_y = dy / tot_y * FastMath.PI * k;
                        LF = Helper.rotar(LF, Camara.LookAt, N, an_y);

                        Camara.SetCamera(LF, Camara.LookAt);


                    }
                }
            }

            if (mouseCaptured)
                Cursor.Position = mouseCenter;


            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_MIDDLE))
            {
                if (eventoInterno == 0)
                {
                    xm = Input.Xpos;
                    ym = Input.Ypos;
                    eventoInterno = 1;
                }
                else
                {
                    float dx = Input.Xpos - xm;
                    float dy = Input.Ypos - ym;
                    xm = Input.Xpos;
                    ym = Input.Ypos;

                    float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftControl) ? 0.5f : 1f;

                    Vector3 VUP = new Vector3(0, 1, 0);
                    Vector3 d = Camara.LookAt - Camara.Position;
                    float dist = d.Length();
                    // mido la pantalla en el plano donde esta el LookAt
                    float fov = FastMath.QUARTER_PI;
                    float aspect = 1;
                    float Width = 1200;
                    float Height = 900;
                    float kx = 2 * FastMath.Tan(fov / 2) * dist * aspect / Width * k;
                    float ky = 2 * FastMath.Tan(fov / 2) * dist / Height * k;
                    d.Normalize();
                    Vector3 n = Vector3.Cross(d, VUP);
                    n.Normalize();
                    Vector3 up = Vector3.Cross(n, d);
                    Vector3 desf = up * (dy * ky) + n * (dx * kx);
                    Camara.SetCamera(Camara.Position + desf, Camara.LookAt + desf);
                }
            }

            float zDelta = Input.WheelPos;
            wm = Input.WheelPos;
            if (FastMath.Abs(zDelta) > 0.1f)
            {
                float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 10 : 100;
                Vector3 LF = Camara.Position;
                Vector3 ViewDir = Camara.LookAt - LF;
                ViewDir.Normalize();

                Vector3 LA = Camara.LookAt;
                float dist = (LA - LF).Length();
                if (zDelta > 0)
                {
                    LF = LF + ViewDir * k;
                }
                else
                {
                    LF = LF - ViewDir * k;
                }
                Camara.SetCamera(LF, Camara.LookAt);
            }



            if (curr_mode == defines.MODO_GAME)
            {
                float k = Input.keyDown(Microsoft.DirectX.DirectInput.Key.LeftShift) ? 0.01f : 0.4f;
                Vector3 ant_ship_pos = ship_pos;
                ship_pos = ship_pos + ship_vel * ElapsedTime * ship_speed * k;
                ship_H = ship_pos.Length() - star_r;

                // chequeo que no choque contra el piso y/o el techo
                float min_H = -17;
                float max_H = -5;
                if (ship_H < min_H || ship_H>max_H)
                {
                    Vector3 N  = ship_pos;
                    N.Normalize();
                    if (ship_H < min_H )
                        ship_pos = N * (star_r + min_H);
                    else
                        ship_pos = N * (star_r + max_H);
                    // lo roto un poco para arriba / abajo para que no siga chocando
                    Matrix rotBT = Matrix.RotationAxis(ship_bitan, ship_H < min_H ? 0.001f : -0.001f);
                    ship_vel.TransformNormal(rotBT);
                    ship_N.TransformNormal(rotBT);
                }

                // determino en que bloque estoy:
                curr_block = 0;
                float min_dist = 100000000;
                for (int i = 0; i < scene.Count; ++i)
                {
                    float dist = (scene[i].Position - ship_pos).LengthSq();
                    if (dist < min_dist)
                    {
                        curr_block = i;
                        min_dist = dist;
                    }
                }


                // colision con obstaculos
                if (r_timer == 0 && explosion_timer==0)
                {
                    Matrix O = Helper.MatrixfromBasis(
                                                        1, 0, 0,
                                                        0, 0, 1,
                                                        0, 1, 0
                                                        );
                    if (Math.Abs(ship_an + ship_an_base) > 0.001f)
                        O = O * Matrix.RotationX(ship_an_base + ship_an);
                    if (Math.Abs(ship_anV) > 0.001f)
                        O = O * Matrix.RotationY(ship_anV);

                    Matrix T = O * Helper.CalcularMatriz(ship_pos, ship_k, ship_vel, ship_bitan, ship_N);
                    Vector3[] p = new Vector3[cant_cpt];
                    for (int s = 0; s < cant_cpt; ++s)
                    {
                        p[s] = new Vector3(0, 0, 0);
                        p[s].TransformCoordinate(Matrix.Translation(collision_pt[s]) * T);
                    }

                    bool colisiona = false;
                    for (int s = 0; s < cant_cpt && !colisiona; ++s)
                    {
                        if (scene[curr_block].colisiona(p[s]))
                        {
                            colisiona = true;
                            // hago explotar la nave
                            explosion_timer = tiempo_explosion;
                            cd_index = s;
                        }
                    }
                }

                if (ship_an != 0)
                {
                    ship_an -= ElapsedTime * 1.5f * Math.Sign(ship_an);
                    if (FastMath.Abs(ship_an) < 0.05f)
                        ship_an = 0;
                }
                if (ship_anV != 0)
                {
                    ship_anV -= ElapsedTime * 1.5f * Math.Sign(ship_anV);
                    if (FastMath.Abs(ship_anV) < 0.05f)
                        ship_anV = 0;
                }
                Vector3 ViewDir = Camara.LookAt - Camara.Position;
                float cam_dist = ViewDir.Length();
                ViewDir.Normalize();
                Vector3 desired_LF = ship_pos - ship_vel * 30 + ship_N * 1.5f;
                Vector3 desired_LA = ship_pos + ship_N * 2.0f;
                Vector3 cam_N = ship_pos;
                cam_N.Normalize();
                Camara.SetCamera(desired_LF, desired_LA, cam_N);
                
                UpdateView();

            }

        }



        public void RenderShadowMap()
        {
            var device = D3DDevice.Instance.Device;
            // Calculo la matriz de view de la luz
            // pongo la luz arriba de la nave
            Vector3 LP = ship_pos + ship_N * 30 - ship_vel*50 - ship_bitan * 10;
            Vector3 LPat = ship_pos ;
            Vector3 LDir = LPat - LP;
            LDir.Normalize();
            effect.SetValue("g_vLightPos", new Vector4(LP.X, LP.Y, LP.Z, 1));
            effect.SetValue("g_vLightDir", new Vector4(LDir.X, LDir.Y, LDir.Z, 1));
            g_LightView = Matrix.LookAtLH(LP, LP + LDir, ship_vel);

            // inicializacion standard: 
            effect.SetValue("g_mProjLight", g_mShadowProj);
            effect.SetValue("g_mViewLightProj", g_LightView * g_mShadowProj);

            // Primero genero el shadow map, para ello dibujo desde el pto de vista de luz
            // a una textura, con el VS y PS que generan un mapa de profundidades. 
            Surface pOldRT = device.GetRenderTarget(0);
            Surface pShadowSurf = g_pShadowMap.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pShadowSurf);
            Surface pOldDS = device.DepthStencilSurface;
            device.DepthStencilSurface = g_pDSShadow;
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.White, 1.0f, 0);
            device.BeginScene();

            // Hago el render de la escena pp dicha
            effect.SetValue("g_txShadow", g_pShadowMap);
            effect.SetValue("texLightMap", g_pLightMap);

            // Dibujo la escena pp dicha. (Solo lo que proyecta sombra)
            effect.Technique = "RenderShadow";
            RenderShip("RenderShadow");

            // Termino 
            device.EndScene();

            //TextureLoader.Save("shadowmap.bmp", ImageFileFormat.Bmp, g_pShadowMap);

            // restuaro el render target y el stencil
            device.DepthStencilSurface = pOldDS;
            device.SetRenderTarget(0, pOldRT);

        }


        public void RenderIntro()
        {
            

            var device = D3DDevice.Instance.Device;
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();

            // fondo
            sprite.Begin(SpriteFlags.AlphaBlend);
            sprite.Transform = Matrix.Transformation2D(new Vector2(0, 0), 0, new Vector2(1, 1), Vector2.Empty, 0, new Vector2(0, 0));
            sprite.Draw(gui_texture[1], new Rectangle(0,0,screen_dx,screen_dy), Vector3.Empty, new Vector3(0, 0, 0),Color.White);
            sprite.Transform = Matrix.Identity;
            sprite.End();

            // gui pp dicho
            gui.Render();
            device.EndScene();
            device.Present();

            if (musicTrack.getStatus() == TgcMp3Player.States.Open)
            {
                musicTrack.play(true);
            }
        }


        public override void Render()
        {
            musicTrack.FileName = MediaDir + "MarchoftheResistance.mp3";

            if (gui_mode)
            {
                RenderIntro();
                return;
            }
            if (!gui_mode && !playMusic)
            {
                musicTrack.stop();
                musicTrack.closeFile();
                musicTrack.FileName = MediaDir + "The Prodigy - Voodoo People (Pendulum Remix).mp3";
                playMusic = true;
            }

            xwingShips.Render();
            
            if (musicTrack.getStatus() == TgcMp3Player.States.Open || musicTrack.getStatus() == TgcMp3Player.States.Stopped )
            {
                musicTrack.play(true);
            }
            
            //Genero el shadow map
            RenderShadowMap();


            var device = D3DDevice.Instance.Device;
            var pOldRT = device.GetRenderTarget(0);
            var pOldDS = device.DepthStencilSurface;
            var pSurf = g_pRenderTarget.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            device.DepthStencilSurface = g_pDepthStencil;
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();

            effect = effectBase;
            RenderScene(shadow_map ? "DefaultTechnique" : "DefaultTechniqueNoShadows");

            if (curr_mode == defines.MODO_GAME)
            {
                effect.Technique = "SkyBox";
                device.VertexFormat = CustomVertex.PositionTextured.Format;
                device.SetStreamSource(0, g_pVBV3D, 0);
                Vector3 ViewDir = Camara.LookAt - Camara.Position;
                ViewDir.Normalize();

                Vector3 Up = curr_mode == defines.MODO_CAMARA ? new Vector3(0, 1, 0) : ship_N;
                Vector3 U, V;
                V = Vector3.Cross(ViewDir, Up);
                V.Normalize();
                U = Vector3.Cross(V, ViewDir);
                U.Normalize();

                float fov = D3DDevice.Instance.FieldOfView;
                float W = device.PresentationParameters.BackBufferWidth;
                float H = device.PresentationParameters.BackBufferHeight;
                float k = 2 * FastMath.Tan(fov / 2) / H;
                Vector3 Dy = U * k;
                Vector3 Dx = V * k;
                float Zn = D3DDevice.Instance.ZNearPlaneDistance;
                float Zf = D3DDevice.Instance.ZFarPlaneDistance;
                float Q = Zf / (Zf - Zn);

                effect.SetValue("LookFrom", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
                effect.SetValue("ViewDir", TgcParserUtils.vector3ToFloat4Array(ViewDir));
                effect.SetValue("Dx", TgcParserUtils.vector3ToFloat4Array(Dx));
                effect.SetValue("Dy", TgcParserUtils.vector3ToFloat4Array(Dy));
                effect.SetValue("MatProjQ", Q);
                effect.SetValue("Zn", Zn);
                effect.SetValue("Zf", Zf);

                effect.Begin(FX.None);
                effect.BeginPass(0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                effect.EndPass();
                effect.End();

                // explosion?
                if (explosion_timer > 0)
                {
                    effect = effectExplosion;
                    float t = (tiempo_explosion - explosion_timer) / tiempo_explosion;
                    effect.Technique = "Explosion";
                    Matrix O = Helper.MatrixfromBasis(
                                            1, 0, 0,
                                            0, 0, 1,
                                            0, 1, 0
                                            );
                    if (Math.Abs(ship_an + ship_an_base) > 0.001f)
                        O = O * Matrix.RotationX(ship_an_base + ship_an);
                    if (Math.Abs(ship_anV) > 0.001f)
                        O = O * Matrix.RotationY(ship_anV);
                    Matrix T = O * Helper.CalcularMatriz(ship_pos, ship_k, ship_vel, ship_bitan, ship_N);
                    Vector3 pt = new Vector3(0, 0, 0);
                    pt.TransformCoordinate(Matrix.Translation(collision_pt[cd_index]) * T);
                    device.SetRenderState(RenderStates.AlphaBlendEnable, false);
                    effect.SetValue("_Sphere", new Vector4(pt.X, pt.Y, pt.Z, 10.0f * t + 0.5f));
                    effect.SetValue("_NoiseAmp", -5f * t);
                    effect.SetValue("_NoiseFreq", 0.2f);
                    //effect.SetValue("_ExploAlpha", 1 - t);
                    effect.SetValue("LookFrom", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
                    effect.SetValue("ViewDir", TgcParserUtils.vector3ToFloat4Array(ViewDir));
                    effect.SetValue("Dx", TgcParserUtils.vector3ToFloat4Array(Dx));
                    effect.SetValue("Dy", TgcParserUtils.vector3ToFloat4Array(Dy));
                    effect.SetValue("MatProjQ", Q);
                    effect.SetValue("Zn", Zn);
                    effect.SetValue("Zf", Zf);
                    effect.Begin(FX.None);
                    effect.BeginPass(2);        // 3 = 32 pasos x explosion
                    device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                    effect.EndPass();
                    effect.End();

                }



            }
            RenderExplosiones();
            device.EndScene();
            pSurf.Dispose();

            effect = effectBase;
            RenderDisparos();

            if (explosion_timer == 0 && glow)
            {
                // glow effect
                // 1er pasada: Genero el glowmap
                // -----------------------------------------------------
                pSurf = g_pRenderTarget4.GetSurfaceLevel(0);
                device.SetRenderTarget(0, pSurf);
                device.BeginScene();
                device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);

                effect.Technique = "GlowMap";
                RenderShip("GlowMap");
                RenderDisparos();
                pSurf.Dispose();
                device.EndScene();

                // Pasadas de blur
                for (int P = 0; P < 1; ++P)
                {
                    // Gaussian blur Horizontal
                    // -----------------------------------------------------
                    pSurf = g_pRenderTarget4Aux.GetSurfaceLevel(0);
                    device.SetRenderTarget(0, pSurf);
                    // dibujo el quad pp dicho :
                    device.BeginScene();
                    effect.Technique = "GaussianBlurSeparable";
                    device.VertexFormat = CustomVertex.PositionTextured.Format;
                    device.SetStreamSource(0, g_pVBV3D, 0);
                    effect.SetValue("g_RenderTarget", g_pRenderTarget4);

                    device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                    effect.Begin(FX.None);
                    effect.BeginPass(0);
                    device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                    effect.EndPass();
                    effect.End();
                    pSurf.Dispose();
                    device.EndScene();

                    pSurf = g_pRenderTarget4.GetSurfaceLevel(0);
                    device.SetRenderTarget(0, pSurf);
                    pSurf.Dispose();

                    //  Gaussian blur Vertical
                    // -----------------------------------------------------
                    device.BeginScene();
                    effect.Technique = "GaussianBlurSeparable";
                    device.VertexFormat = CustomVertex.PositionTextured.Format;
                    device.SetStreamSource(0, g_pVBV3D, 0);
                    effect.SetValue("g_RenderTarget", g_pRenderTarget4Aux);

                    device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
                    effect.Begin(FX.None);
                    effect.BeginPass(1);
                    device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                    effect.EndPass();
                    effect.End();
                    device.EndScene();

                }
            }

            // Post procesado final
            // --------------------------------------------------------------------------
            device.DepthStencilSurface = pOldDS;
            device.SetRenderTarget(0, pOldRT);
            device.BeginScene();
            effect.Technique = "PostProcess";
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.SetStreamSource(0, g_pVBV3D, 0);
            effect.SetValue("g_RenderTarget", g_pRenderTarget);
            effect.SetValue("g_RenderTarget4", g_pRenderTarget4);
            effect.SetValue("g_Position", g_pPosition);
            effect.SetValue("g_Normal", g_pNormal);
            effect.SetValue("matProj", device.Transform.Projection);
            effect.SetValue("fish_kU", 0.1f);
            effect.SetValue("glow_factor", glow?8.0f:0.0f);

            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            effect.Begin(FX.None);
            effect.BeginPass(0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            effect.EndPass();
            effect.End();
            device.EndScene();
            // --------------------------------------------------------------------------

            // dibujo el scoreboard, tiempo, vidas, etc (y fps)
            RenderHUD();

            if (opciones_dx)
            {
                device.BeginScene();
                gui.Render();
                device.EndScene();
            }

            device.Present();
        }

     

        public void RenderHUD()
        {
            var device = D3DDevice.Instance.Device;
            device.BeginScene();
            if (curr_mode == defines.MODO_GAME)
            {
                if (intro_timer > 0)
                {
                    FillText(screen_dx / 2, screen_dy / 2, (int)intro_timer + "s para comenzar...", Color.Yellow, true);
                }
                else
                {
                    int x0 = (int)(screen_dx * 0.8f);
                    int dx = 30;
                    int y0 = (int)(screen_dy * 0.2f);
                    int dy = (int)(screen_dy * 0.6f); ;
                    DrawRect(x0, y0, x0 + dx, y0 + dy, 1, Color.WhiteSmoke);
                    float ex = (float)dx / 20.0f;
                    int cant_b = 5;
                    float ey = (float)dy / (Block.largo * cant_b) * 0.8f;
                    int db = (int)(Block.largo / (float)cant_b);
                    x0 += dx / 2;
                    y0 += dy - db / 2;

                    Vector3 pt = ship_pos;
                    float dist0 = FastMath.Atan2(ship_pos.Y, ship_pos.Z) * star_r;

                    // scoreboard
                    Color verde = Color.FromArgb(60, 133, 59);
                    sprite.Begin(SpriteFlags.AlphaBlend);
                    sprite.Transform = Matrix.Transformation2D(new Vector2(0, 0), 0, new Vector2(1, (float)screen_dx / (float)screen_dy), Vector2.Empty, 0, new Vector2(0, 0));
                    sprite.Draw(gui_texture[0], Rectangle.Empty, Vector3.Empty, new Vector3(screen_dx - 590, 0, 0),
                            Color.White);
                    sprite.Transform = Matrix.Identity;
                    sprite.End();
                    FillText(screen_dx - 550, 16, String.Format("{0:N}", Math.Abs(Math.Round(dist0, 3))), verde);
                    FillText(screen_dx - 420, 16, "" + vidas, verde);
                    FillText(screen_dx - 320, 16, "0", verde);

                    int min = (int)(time / 60);
                    int seg = (int)(time % 60);
                    FillText(screen_dx - 150, 16, min + ":" + seg, verde);



                    // preview:
                    // nave
                    FillRect(x0 - 5, y0 - 5, x0 + 5, y0 + 5, Color.Red);
                    for (int s = 0; s < cant_b; ++s)
                    {
                        Block B = scene[curr_block - s];
                        for (int i = 0; i < B.cant_mesh; ++i)
                        {
                            if (B.mesh_type[i] == 1)
                            {
                                Vector3 pmin = B.pmin[i];
                                Vector3 pmax = B.pmax[i];
                                pmin.TransformCoordinate(B.matWorldBlock);
                                pmax.TransformCoordinate(B.matWorldBlock);
                                float d0 = FastMath.Atan2(pmin.Y, pmin.Z) * star_r;
                                float d1 = FastMath.Atan2(pmax.Y, pmax.Z) * star_r;

                                float X0 = Math.Abs(d0 - dist0);
                                float X1 = Math.Abs(d1 - dist0);
                                FillRect(x0 + pmin.X * ex, y0 - X0 * ey - 3,
                                             x0 + pmax.X * ex, y0 - X1 * ey + 3,
                                             Color.Beige);
                            }
                        }
                        y0 -= db;
                    }
                }
            }
            RenderFPS();
            device.EndScene();

        }


        public void RenderExplosiones()
        {
            var device = D3DDevice.Instance.Device;
            device.SetRenderState(RenderStates.AlphaBlendEnable, true);
            for (int i=0;i<explosiones.Length;++i)
            {
                if (explosiones[i].timer > 0)
                {
                    explosiones[i].timer -= ElapsedTime;
                    if (explosiones[i].timer < 0)
                        explosiones[i].timer = 0;
                    else
                    {
                        // dibujo la explosion pp dich
                        effect = effectExplosion;
                        float t = (explosiones[i].tiempo_explosion - explosiones[i].timer) / explosiones[i].tiempo_explosion;
                        effect.Technique = "Explosion";
                        Vector3 pt = explosiones[i].Position;
                        effect.SetValue("_Sphere", new Vector4(pt.X, pt.Y, pt.Z, 4.0f * t + 1.0f ));
                        effect.SetValue("_NoiseAmp", -10f * t);
                        effect.SetValue("_NoiseFreq", 0.2f);
                        effect.SetValue("_ExploAlpha", 1-t);
                        device.VertexFormat = CustomVertex.PositionTextured.Format;
                        device.SetStreamSource(0, g_pVBV3D, 0);
                        Vector3 ViewDir = Camara.LookAt - Camara.Position;
                        ViewDir.Normalize();
                        Vector3 Up = curr_mode == defines.MODO_CAMARA ? new Vector3(0, 1, 0) : ship_N;
                        Vector3 U, V;
                        V = Vector3.Cross(ViewDir, Up);
                        V.Normalize();
                        U = Vector3.Cross(V, ViewDir);
                        U.Normalize();
                        float fov = D3DDevice.Instance.FieldOfView;
                        float W = device.PresentationParameters.BackBufferWidth;
                        float H = device.PresentationParameters.BackBufferHeight;
                        float k = 2 * FastMath.Tan(fov / 2) / H;
                        Vector3 Dy = U * k;
                        Vector3 Dx = V * k;
                        float Zn = D3DDevice.Instance.ZNearPlaneDistance;
                        float Zf = D3DDevice.Instance.ZFarPlaneDistance;
                        float Q = Zf / (Zf - Zn);

                        effect.SetValue("LookFrom", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
                        effect.SetValue("ViewDir", TgcParserUtils.vector3ToFloat4Array(ViewDir));
                        effect.SetValue("Dx", TgcParserUtils.vector3ToFloat4Array(Dx));
                        effect.SetValue("Dy", TgcParserUtils.vector3ToFloat4Array(Dy));
                        effect.SetValue("MatProjQ", Q);
                        effect.SetValue("Zn", Zn);
                        effect.SetValue("Zf", Zf);
                        effect.Begin(FX.None);
                        effect.BeginPass(1);            // 1 = 8 pasos x explosion
                        device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                        effect.EndPass();
                        effect.End();
                    }
                }
            }
        }



        public void UpdateDisparos()
        {
            float speed = 300;
            for (int i = 0; i < disparos.Length; ++i)
            {
                disparos[i].draw = false;
                if (disparos[i].delay > 0)
                {
                    disparos[i].delay -= ElapsedTime;
                }
                else
                if (disparos[i].timer > 0)
                {
                    disparos[i].timer -= ElapsedTime;
                    if (disparos[i].timer < 0)
                    {
                        disparos[i].timer = 0;      // termina el disparo
                    }
                    else
                    {
                        Vector3 p0 = disparos[i].Position;
                        if (!disparos[i].enemigo)
                        {
                            // en que bloque esta el disparo
                            int fire_block = curr_block;
                            float min_dist = (scene[curr_block].Position - p0).LengthSq();
                            for (int t = 1; t < 5; ++t)
                            {
                                float dist = (scene[curr_block - t].Position - p0).LengthSq();
                                if (dist < min_dist)
                                {
                                    fire_block = curr_block - t;
                                    min_dist = dist;
                                }
                            }
                            disparos[i].block = fire_block;
                            if (scene[fire_block].colisiona(p0))
                            {
                                disparos[i].timer = 0;      // termina el disparo
                                explosiones[p_explosion].tiempo_explosion = explosiones[p_explosion].timer = 2.0f;
                                explosiones[p_explosion].Position = p0;
                                if (++p_explosion >= explosiones.Length)
                                    p_explosion = 0;
                            }
                            else
                            {
                                // lo marco para dibujar
                                disparos[i].draw = true;
                            }
                        }
                        else
                        {
                            // lo marco para dibujar
                            disparos[i].draw = true;
                        }
                    }
                }
                // actualizo la pos. del disparo
                disparos[i].Position += disparos[i].Velocity * speed * ElapsedTime;

            }
        }

        public void RenderDisparos()
        {
            var device = D3DDevice.Instance.Device;
            for (int i = 0; i < disparos.Length; ++i)
            {
                if (disparos[i].draw)
                {
                    Vector3 p0 = disparos[i].Position;
                    Vector3 p1 = p0 + disparos[i].Velocity * 50;
                    DrawLine(p0, p1, ship_N, 0.1f,
                        disparos[i].enemigo ?   Color.FromArgb(255, 0, 255, 255) : 
                                                Color.FromArgb(255, 255, 255, 0));
                }
            }
        }


        public void RenderBloques()
        {

            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(ship_pos + ship_N*1000));
            effect.SetValue("texDiffuseMap", textura_bloques);
            effect.SetValue("ssao", ssao?1:0);
            effect.Begin(0);
            effect.BeginPass(0);
            foreach (Block bloque in scene)
            {
                bloque.render();
            }
            effect.EndPass();
            effect.End();
        }


        public void RenderScene(String technique)
        {
            var device = D3DDevice.Instance.Device;
            effect.SetValue("eyePosition", TgcParserUtils.vector3ToFloat4Array(Camara.Position));
            effect.Technique = technique;

            if (curr_mode != defines.MODO_CAMARA)
                RenderBloques();

            bool render_ship = curr_mode != defines.MODO_TEST_BLOCK? true : false;
            if (render_ship && r_timer != 0)
                render_ship = (int)(r_timer * 1000) % 2 == 0 ? true : false;

         

            if (render_ship)
            {
                RenderShip(technique);
            }
        }

        public void RenderShip(String technique)
        {
            // render ship
            Matrix O = Helper.MatrixfromBasis(
                                    1, 0, 0,
                                    0, 0, 1,
                                    0, 1, 0
                                    );
            if (Math.Abs(ship_an + ship_an_base) > 0.001f)
                O = O * Matrix.RotationX((ship_an_base + ship_an) );
            if (Math.Abs(ship_anV) > 0.001f)
                O = O * Matrix.RotationY(ship_anV * 0.15f);

            Matrix T = O * Helper.CalcularMatriz(ship_pos, ship_k, ship_vel, ship_bitan, ship_N);
            effect.SetValue("ssao", 0);
            Vector3 LP = ship_pos + ship_vel * 220 + ship_N * 150;
            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(LP));


            if (technique == "GlowMap" )
            {
                TgcShaders.Instance.setShaderMatrix(effect, T);
                effect.SetValue("glow_color", new Vector4(1,0.3f,0.3f,1));
                effect.Begin(FX.None);
                effect.BeginPass(0);

                // 15 = motores
                xwing[0].D3dMesh.DrawSubset(15);
                xwing[1].D3dMesh.DrawSubset(15);

                effect.EndPass();
                effect.End();
            }
            else
            {
                foreach (TgcMesh mesh in xwing)
                {
                    mesh.Transform = T;
                    mesh.Technique = technique;
                    mesh.render();
                }
            }


            effect.SetValue("lightPosition", TgcParserUtils.vector3ToFloat4Array(LightPos));
        }

        public void DrawLine(Vector3 p0, Vector3 p1, Vector3 up,float dw, Color color)
        {
        
            Vector3 v = p1 - p0;
            v.Normalize();
            Vector3 n = Vector3.Cross(v, up);
            Vector3 w = Vector3.Cross(n, v);

            Vector3[] p = new Vector3[8];

            dw *= 0.5f;
            p[0] = p0 - n * dw;
            p[1] = p1 - n * dw;
            p[2] = p1 + n * dw;
            p[3] = p0 + n * dw;
            for (int i = 0; i < 4; ++i)
            {
                p[4 + i] = p[i] + w * dw;
                p[i] -= w * dw;
            }

            int[] index_buffer = { 0, 1, 2, 0, 2, 3, 
                                       4, 5, 6, 4, 6, 7, 
                                       0, 1, 5, 0, 5, 4, 
                                       3, 2, 6, 3, 6, 7 };

            VERTEX_POS_COLOR[] pt = new VERTEX_POS_COLOR[index_buffer.Length];
            for(int i=0;i< index_buffer.Length;++i)
            {
                int index = index_buffer[i];
                pt[i].x = p[index].X;
                pt[i].y = p[index].Y;
                pt[i].z = p[index].Z;
                pt[i].color = color.ToArgb();
            }
         
            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Position | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, index_buffer.Length/3, pt);
        }


        // line 2d
        public void DrawLine(float x0, float y0, float x1, float y1, int dw, Color color)
        {
            Vector2[] V = new Vector2[4];
            V[0].X = x0;
            V[0].Y = y0;
            V[1].X = x1;
            V[1].Y = y1;

            if (dw < 1)
                dw = 1;

            // direccion normnal
            Vector2 v = V[1] - V[0];
            v.Normalize();
            Vector2 n = new Vector2(-v.Y, v.X);

            V[2] = V[1] + n * dw;
            V[3] = V[0] + n * dw;

            VERTEX2D[] pt = new VERTEX2D[16];
            // 1er triangulo
            pt[0].x = V[0].X;
            pt[0].y = V[0].Y;
            pt[1].x = V[1].X;
            pt[1].y = V[1].Y;
            pt[2].x = V[2].X;
            pt[2].y = V[2].Y;

            // segundo triangulo
            pt[3].x = V[0].X;
            pt[3].y = V[0].Y;
            pt[4].x = V[2].X;
            pt[4].y = V[2].Y;
            pt[5].x = V[3].X;
            pt[5].y = V[3].Y;

            for (int t = 0; t < 6; ++t)
            {
                pt[t].z = 0.5f;
                pt[t].rhw = 1;
                pt[t].color = color.ToArgb();
                ++t;
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Transformed | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 2, pt);
        }

        public void DrawRect(float x0, float y0, float x1, float y1, int dw, Color color)
        {
            DrawLine(x0, y0, x1, y0, dw, color);
            DrawLine(x0, y1, x1, y1, dw, color);
            DrawLine(x0, y0, x0, y1, dw, color);
            DrawLine(x1, y0, x1, y1, dw, color);
        }

        public void FillRect(float x0, float y0, float x1, float y1, Color color)
        {
            Vector2[] V = new Vector2[4];
            V[0].X = x0;
            V[0].Y = y0;
            V[1].X = x0;
            V[1].Y = y1;
            V[2].X = x1;
            V[2].Y = y1;
            V[3].X = x1;
            V[3].Y = y0;

            VERTEX2D[] pt = new VERTEX2D[16];
            // 1er triangulo
            pt[0].x = V[0].X;
            pt[0].y = V[0].Y;
            pt[1].x = V[1].X;
            pt[1].y = V[1].Y;
            pt[2].x = V[2].X;
            pt[2].y = V[2].Y;

            // segundo triangulo
            pt[3].x = V[0].X;
            pt[3].y = V[0].Y;
            pt[4].x = V[2].X;
            pt[4].y = V[2].Y;
            pt[5].x = V[3].X;
            pt[5].y = V[3].Y;

            for (int t = 0; t < 6; ++t)
            {
                pt[t].z = 0.5f;
                pt[t].rhw = 1;
                pt[t].color = color.ToArgb();
                ++t;
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Transformed | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 2, pt);
        }


        public void FillText(int x, int y, string text , Color color , bool center=false)
        {
            var device = D3DDevice.Instance.Device;
            // elimino cualquier textura que me cague el modulate del vertex color
            device.SetTexture(0, null);
            // Desactivo el zbuffer
            bool ant_zenable = device.RenderState.ZBufferEnable;
            device.RenderState.ZBufferEnable = false;
            // pongo la matriz identidad
            Matrix matAnt = sprite.Transform * Matrix.Identity;
            sprite.Transform = Matrix.Identity;
            sprite.Begin(SpriteFlags.AlphaBlend);
            if (center)
            {
                Rectangle rc = new Rectangle(0, y, screen_dx, y + 100);
                font.DrawText(sprite, text, rc, DrawTextFormat.Center, color);
            }
            else
            {
                Rectangle rc = new Rectangle(x, y, x + 600, y + 100);
                font.DrawText(sprite, text, rc, DrawTextFormat.NoClip | DrawTextFormat.Top | DrawTextFormat.Left, color);
            }
            sprite.End();
            // Restauro el zbuffer
            device.RenderState.ZBufferEnable = ant_zenable;
            // Restauro la transformacion del sprite
            sprite.Transform = matAnt;
        }


        /// <summary>
        ///     Se llama cuando termina la ejecución del ejemplo.
        ///     Hacer Dispose() de todos los objetos creados.
        ///     Es muy importante liberar los recursos, sobretodo los gráficos ya que quedan bloqueados en el device de video.
        /// </summary>
        public override void Dispose()
        {
            Box.dispose();
            blaster.dispose();
            tieFighter.dispose();
            foreach (TgcMesh mesh in meshes)
                mesh.dispose();
        }
    }

    public class Helper
    {
        // helpers varios
        static public Matrix CalcularMatriz(Vector3 Pos, Vector3 Scale, Vector3 Dir)
        {
            // determino la orientacion
            Vector3 U = Vector3.Cross(new Vector3(0, 1, 0), Dir);
            U.Normalize();
            if (FastMath.Abs(U.X) < 0.001f && FastMath.Abs(U.Y) < 0.001f && FastMath.Abs(U.Z) < 0.001f)
                U = Vector3.Cross(new Vector3(0, 0, 1), Dir);
            Vector3 V = Vector3.Cross(Dir, U);
            Matrix matWorld = Matrix.Scaling(Scale);
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * Matrix.Translation(Pos);
            return matWorld;
        }


        static public Matrix CalcularMatriz(Vector3 Pos, Vector3 Scale, Vector3 U, Vector3 V, Vector3 N)
        {
            Matrix matWorld = Matrix.Scaling(Scale);
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = N.X;
            Orientacion.M32 = N.Y;
            Orientacion.M33 = N.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * Matrix.Translation(Pos);
            return matWorld;
        }



        static public Matrix CalcularUVN(Vector3 Dir)
        {
            // determino la orientacion
            Dir.Normalize();
            Vector3 U = Vector3.Cross(new Vector3(1, 0, 0), Dir);
            if (FastMath.Abs(U.X) < 0.001f && FastMath.Abs(U.Y) < 0.001f && FastMath.Abs(U.Z) < 0.001f)
                U = Vector3.Cross(new Vector3(0, 1, 0), Dir);
            U.Normalize();
            Vector3 V = Vector3.Cross(Dir, U);
            V.Normalize();
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M31 = V.X;
            Orientacion.M32 = V.Y;
            Orientacion.M33 = V.Z;
            Orientacion.M34 = 0;

            Orientacion.M21 = Dir.X;
            Orientacion.M22 = Dir.Y;
            Orientacion.M23 = Dir.Z;
            Orientacion.M24 = 0;


            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            return Orientacion;
        }

        static public Matrix CalcularUVN(Vector3 Dir, Vector3 Up)
        {
            // determino la orientacion
            Dir.Normalize();
            Vector3 U = Vector3.Cross(Up, Dir);
            U.Normalize();
            Vector3 V = Vector3.Cross(Dir, U);
            V.Normalize();
            Matrix Orientacion;
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M31 = V.X;
            Orientacion.M32 = V.Y;
            Orientacion.M33 = V.Z;
            Orientacion.M34 = 0;

            Orientacion.M21 = Dir.X;
            Orientacion.M22 = Dir.Y;
            Orientacion.M23 = Dir.Z;
            Orientacion.M24 = 0;


            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            return Orientacion;
        }


        static public Matrix MatrixfromBasis(float Ux, float Uy, float Uz,
                                                float Vx, float Vy, float Vz,
                                                float Wx, float Wy, float Wz)
        {
            Matrix O = new Matrix();
            O.M11 = Ux; O.M12 = Uy; O.M13 = Uz; O.M14 = 0;
            O.M21 = Vx; O.M22 = Vy; O.M23 = Vz; O.M24 = 0;
            O.M31 = Wx; O.M32 = Wy; O.M33 = Wz; O.M34 = 0;
            O.M41 = 0; O.M42 = 0; O.M43 = 0; O.M44 = 1;
            return O;
        }

        static public Vector3 rotar(Vector3 A, Vector3 o, Vector3 eje, float theta)
        {
            float x = A.X;
            float y = A.Y;
            float z = A.Z;
            float a = o.X;
            float b = o.Y;
            float c = o.Z;
            float u = eje.X;
            float v = eje.Y;
            float w = eje.Z;

            float u2 = u * u;
            float v2 = v * v;
            float w2 = w * w;
            float cosT = FastMath.Cos(theta);
            float sinT = FastMath.Sin(theta);
            float l2 = u2 + v2 + w2;
            float l = FastMath.Sqrt(l2);

            if (l2 < 0.000000001f)       // el vector de rotacion es casi nulo
                return A;

            float xr = a * (v2 + w2) + u * (-b * v - c * w + u * x + v * y + w * z)
                    + (-a * (v2 + w2) + u * (b * v + c * w - v * y - w * z) + (v2 + w2) * x) * cosT
                    + l * (-c * v + b * w - w * y + v * z) * sinT;
            xr /= l2;

            float yr = b * (u2 + w2) + v * (-a * u - c * w + u * x + v * y + w * z)
                    + (-b * (u2 + w2) + v * (a * u + c * w - u * x - w * z) + (u2 + w2) * y) * cosT
                    + l * (c * u - a * w + w * x - u * z) * sinT;
            yr /= l2;

            float zr = c * (u2 + v2) + w * (-a * u - b * v + u * x + v * y + w * z)
                    + (-c * (u2 + v2) + w * (a * u + b * v - u * x - v * y) + (u2 + v2) * z) * cosT
                    + l * (-b * u + a * v - v * x + u * y) * sinT;
            zr /= l2;

            return new Vector3(xr, yr, zr);
        }
    }
}




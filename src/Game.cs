//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace BlockPuzzle {

    public class Game : GameWindow {
        private Random random;
        private DrawContext drawContext;
        private GameScreen gameScreen;
        private InputManager inputManager;
        private List<GraphicsObject> objectsToDispose;
        private static Dictionary<IGraphicsContext, Game> activeGames =
            new Dictionary<IGraphicsContext, Game>();

        public Game() : base(800, 600, new GraphicsMode(32, 24, 0, 4),
                             "BlockPuzzle", GameWindowFlags.Default)
        {
            drawContext = new DrawContext();
            inputManager = new InputManager(this);
            inputManager.LoadDefaults();
            random = new Random();

            objectsToDispose = new List<GraphicsObject>();
            lock (activeGames)
                activeGames[Context] = this;
        }

        ~Game()
        {
            lock (activeGames)
                activeGames.Remove(Context);
        }

        public static Game ForContext(IGraphicsContext context)
        {
            lock (activeGames) {
                Game rv = null;
                activeGames.TryGetValue(context, out rv);
                return rv;
            }
        }

        public void RegisterForDisposal(GraphicsObject obj)
        {
            lock (objectsToDispose)
                objectsToDispose.Add(obj);
        }

        protected void DisposeObjects()
        {
            // no need to lock if this thing is empty
            if (objectsToDispose.Count == 0)
                return;
            lock (objectsToDispose) {
                foreach (GraphicsObject obj in objectsToDispose)
                    obj.ReleaseResourcesSafely();
                objectsToDispose.Clear();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ColorMaterial);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GameScreen = new MainMenu(this);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            gameScreen.OnUpdate(e);
            inputManager.Update(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            gameScreen.OnRender(drawContext);
            SwapBuffers();
            DisposeObjects();
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            Matrix4 view = Matrix4.CreateOrthographicOffCenter(
                0.0f, InternalWidth, InternalHeight, 0.0f, -1.0f, 100.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref view);
        }

        protected override void OnUnload(EventArgs e)
        {
            DisposeObjects();
        }

        public void OnActionTriggered(InputAction action)
        {
            gameScreen.OnActionTriggered(action);
        }

        public void OnActionReleased(InputAction action)
        {
            gameScreen.OnActionReleased(action);
        }

        public int InternalWidth {
            get { return 800; }
        }
        public int InternalHeight {
            get { return 600; }
        }

        public Random Random {
            get { return random; }
        }

        public GameScreen GameScreen {
            get { return gameScreen; }
            set {
                gameScreen = value;
                GC.Collect();
            }
        }

        public InputManager InputManager {
            get { return inputManager; }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            // TODO: make this intelligent :)
            Directory.SetCurrentDirectory("../../");

            using (Game game = new Game())
                game.Run(60.0);
        }
    }
}

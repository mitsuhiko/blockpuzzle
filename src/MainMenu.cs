//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace BlockPuzzle {

    /// <summary>
    /// Represents a menu item for the main menu.
    /// </summary>
    public class MainMenuItem {
        public delegate void TriggerDelegate();
        private Texture texture;
        private int x;
        private int y;
        private int width;
        private int height;
        private TriggerDelegate trigger;

        public MainMenuItem(TriggerDelegate trigger, Texture texture, int x, int y,
                            int width, int height)
        {
            this.texture = texture;
            this.trigger = trigger;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public void Trigger()
        {
            if (trigger != null)
                trigger();
        }

        public Texture Texture {
            get { return texture; }
        }
        public Vector2 Position {
            get { return new Vector2(x, y); }
        }
        public int Width {
            get { return width; }
        }
        public int Height {
            get { return height; }
        }
    }

    /// <summary>
    /// A game screen that renders and handles the main menu.
    /// </summary>
    public class MainMenu : GameScreen {
        private Texture logo;
        private Texture menuTexture;
        private Texture backgroundTexture;
        private float logoAngle;
        private float itemJump;
        private float menuAngle;
        private float logoDir;
        private float itemDir;
        private float itemAngle;
        private float backgroundScale;
        private Random rnd;
        private int activeItem;
        private List<MainMenuItem> menuItems;

        public MainMenu(Game game) : base(game)
        {
            rnd = new Random();
            logo = new Texture("resources/logo.png");
            menuTexture = new Texture("resources/menu.png");
            backgroundTexture = new Texture("resources/background.png");
            backgroundScale = 1.3f;

            menuItems = new List<MainMenuItem>();
            AddMenuItem(0, 0, 235, 36, OnNewGame);
            AddMenuItem(0, 55, 175, 104, null);
            AddMenuItem(0, 112, 134, 151, null);
            AddMenuItem(0, 170, 98, 218, OnQuit);
        }

        /// <summary>
        /// Callback for the new game button.
        /// </summary>
        protected void OnNewGame()
        {
            Game.GameScreen = new GameSession(Game);
        }

        /// <summary>
        /// Callback for game quitting.
        /// </summary>
        protected void OnQuit()
        {
            Game.Exit();
        }

        /// <summary>
        /// Internal helper for registering a new menu item.
        /// </summary>
        protected void AddMenuItem(int x1, int y1, int x2, int y2,
                                   MainMenuItem.TriggerDelegate trigger)
        {
            int width = x2 - x1;
            int height = y2 - y1;
            menuItems.Add(new MainMenuItem(trigger, menuTexture.Slice(x1, y1, width, height),
                630 - width / 2, 310 + menuItems.Count * 60, width, height));
        }

        public override void OnUpdate(FrameEventArgs args)
        {
            logoDir = (logoDir + (float)(args.Time * rnd.NextDouble() * 5.0)) % 360.0f;
            logoAngle += (float)args.Time * (float)Math.Cos(logoDir) * 6.0f;
            itemDir = (itemDir + (float)(args.Time) * 15.0f) % 360.0f;
            itemJump += (float)args.Time * (float)Math.Cos(itemDir) * 13.0f;
            itemAngle += (float)args.Time * (float)Math.Sin(itemDir) * 15.0f;
            menuAngle += (float)args.Time * (float)Math.Cos(logoDir * 0.5f) * 2.0f;
            backgroundScale += (float)args.Time * (float)Math.Sin(logoDir) * 0.05f;
        }

        public override void OnActionTriggered(InputAction action)
        {
            switch (action) {
            case InputAction.Up:
                ActivateItem(activeItem - 1);
                break;
            case InputAction.Down:
                ActivateItem(activeItem + 1);
                break;
            case InputAction.Cancel:
                Game.Exit();
                break;
            case InputAction.Confirm:
                menuItems[activeItem].Trigger();
                break;
            }
        }

        /// <summary>
        /// Moves the cursor to another menu item.  This will make sure it cannot
        /// move to items outside of the range.
        /// </summary>
        protected void ActivateItem(int item)
        {
            int max = menuItems.Count - 1;
            activeItem = item;
            if (activeItem < 0)
                activeItem = 0;
            else if (activeItem > max)
                activeItem = max;
            itemDir = 0.0f;
            itemJump = 0.0f;
            itemAngle = 0.0f;
        }

        public override void OnRender(DrawContext ctx)
        {
            ctx.Push();
            ctx.ScaleAroundPoint(backgroundScale, backgroundScale, 400.0f, 300.0f);
            ctx.BindTexture(backgroundTexture);
            ctx.DrawTexturedRectangle(800, 600, backgroundTexture);
            ctx.Pop();

            ctx.Push();
            ctx.Translate(50.0f, 50.0f);
            ctx.RotateAroundPoint(logoAngle, 200.0f, 200.0f);
            ctx.BindTexture(logo);
            ctx.DrawTexturedRectangle(400.0f, 400.0f, logo);
            ctx.Pop();

            ctx.Push();
            ctx.RotateAroundPoint(menuAngle, 400.0f, 450.0f);
            ctx.BindTexture(menuTexture);
            int i = 0;
            foreach (MainMenuItem item in menuItems) {
                bool active = i++ == activeItem;
                ctx.Push();
                ctx.Translate(item.Position);
                if (active) {
                    ctx.Translate(0.0f, itemJump);
                    ctx.RotateAroundPoint(itemAngle, item.Width / 2.0f, item.Height / 2.0f);
                }
                ctx.DrawTexturedRectangle(item.Width, item.Height, item.Texture);
                ctx.Pop();
            }
            ctx.Pop();
        }
    }
}


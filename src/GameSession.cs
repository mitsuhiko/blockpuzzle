//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace BlockPuzzle {

    public class GameSession : GameScreen {
        private static List<Color4> regularBlockColors;
        private static List<Color4> hardBlockColors;
        private Texture backgroundTexture;
        private Texture barTexture;
        private Texture gameOverTexture;
        private GamePane leftPane;
        private GamePane rightPane;
        private float backgroundJitter;
        private bool running;

        static GameSession()
        {
            regularBlockColors = new List<Color4>();
            regularBlockColors.Add(Color4.White);
            regularBlockColors.Add(Color4.Cyan);
            regularBlockColors.Add(Color4.PaleGreen);
            regularBlockColors.Add(Color4.Orange);
            regularBlockColors.Add(Color4.PaleVioletRed);
            regularBlockColors.Add(Color4.Chocolate);

            hardBlockColors = new List<Color4>();
            hardBlockColors.Add(new Color4(70, 70, 70, 255));
            hardBlockColors.Add(new Color4(60, 60, 60, 255));
            hardBlockColors.Add(new Color4(50, 50, 50, 255));
        }

        public GameSession(Game game) : base(game)
        {
            backgroundTexture = new Texture("resources/background.png");
            Texture gameTexture = new Texture("resources/blocks.png");
            Texture[] textures = CreateBlockTextures(gameTexture);
            GameBlock[] regularBlocks = CreateColoredBlocks(textures, regularBlockColors, false);
            GameBlock[] hardBlocks = CreateColoredBlocks(textures, hardBlockColors, true);
            barTexture = gameTexture.Slice(6, 88, 243, 66);
            gameOverTexture = gameTexture.Slice(5, 166, 238, 45);

            leftPane = new GamePane(this, regularBlocks, hardBlocks, 40.0f, 30.0f);
            rightPane = new GamePane(this, regularBlocks, hardBlocks, 430.0f, 30.0f);
            running = true;
        }

        protected Texture[] CreateBlockTextures(Texture gameTexture)
        {
            List<Texture> blockTextures = new List<Texture>();
            for (int row = 0; row < 2; row++)
                for (int column = 0; column < 6; column++)
                    blockTextures.Add(gameTexture.Slice(5 + column * 40, 5 + row * 40, 34, 34));
            return blockTextures.ToArray();
        }

        protected GameBlock[] CreateColoredBlocks(Texture[] textures, List<Color4> colors,
                                                  bool hard)
        {
            List<GameBlock> blocks = new List<GameBlock>();
            foreach (Texture texture in textures)
                foreach (Color4 color in colors)
                    blocks.Add(new GameBlock(texture, color, hard));
            return blocks.ToArray();
        }

        public override void OnActionTriggered(InputAction action)
        {
            switch (action) {
            case InputAction.Cancel:
                Game.GameScreen = new MainMenu(Game);
                break;
            case InputAction.Left:
                leftPane.MoveCursorLeft();
                break;
            case InputAction.Right:
                leftPane.MoveCursorRight();
                break;
            case InputAction.Up:
                leftPane.MoveCursorUp();
                break;
            case InputAction.Down:
                leftPane.MoveCursorDown();
                break;
            case InputAction.ExchangeBlock:
                leftPane.PerformExchange();
                break;
            case InputAction.RequestLine:
                leftPane.FastScrollMode = true;
                break;
            case InputAction.DebugAction1:
                leftPane.DropHardLine();
                break;
            }
        }

        public override void OnActionReleased(InputAction action)
        {
            switch (action) {
            case InputAction.RequestLine:
                leftPane.FastScrollMode = false;
                break;
            }
        }

        public void OnBlocksCleared(GamePane pane, int blockCount, int lineCount)
        {
            GamePane otherPane = GetOtherPane(pane);
            if (lineCount > 1)
                otherPane.DropHardLine();
            if (blockCount >= 5)
                pane.DecreaseSpeed();
            if (blockCount >= 8)
                otherPane.IncreaseSpeed();
        }

        public void OnGameOver(GamePane pane)
        {
            running = false;
        }

        public override void OnUpdate(FrameEventArgs args)
        {
            backgroundJitter = (backgroundJitter + (float)args.Time) % 180.0f;
            leftPane.Update(args);
            rightPane.Update(args);
        }

        public override void OnRender(DrawContext ctx)
        {
            float scale = 1.1f + (float)Math.Sin(backgroundJitter) * 0.05f;
            float angle = (float)Math.Cos(backgroundJitter) * 2.0f;
            ctx.Push();
            ctx.ScaleAroundPoint(scale, scale, 400.0f, 300.0f);
            ctx.RotateAroundPoint(angle, 400.0f, 300.0f);
            ctx.BindTexture(backgroundTexture);
            ctx.DrawTexturedRectangle(800.0f, 600.0f, backgroundTexture);
            ctx.Pop();
            leftPane.Draw(ctx);
            rightPane.Draw(ctx);
        }

        public GamePane GetOtherPane(GamePane pane)
        {
            if (pane == leftPane)
                return rightPane;
            else if (pane == rightPane)
                return leftPane;
            return null;
        }

        public Texture BarTexture {
            get { return barTexture; }
        }
        public Texture GameOverTexture {
            get { return gameOverTexture; }
        }
        public bool Running {
            get { return running; }
        }
    }
}


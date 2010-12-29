//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace BlockPuzzle {

    public abstract class GameScreen {
        private Game game;

        public GameScreen(Game game)
        {
            this.game = game;
        }

        public Game Game {
            get { return game; }
        }

        public virtual void OnRender(DrawContext ctx)
        {
        }

        public virtual void OnUpdate(FrameEventArgs args)
        {
        }

        public virtual void OnActionTriggered(InputAction action)
        {
        }

        public virtual void OnActionReleased(InputAction action)
        {
        }
    }
}


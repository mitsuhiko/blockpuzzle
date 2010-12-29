//  Copyright (c) 2010, Armin Ronacher
//  Licensed under the BSD license, see LICENSE for more information

using System;
using System.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;


namespace BlockPuzzle {

    /// <summary>
    /// Because finalization can happen from a lot of threads we have a different
    /// concept for cleanup.  Whenever finalization happens we remember the object
    /// for a little longer and let the game mainloop clean up the mess for us.
    /// If this is no longer possible because the game already vanished we just
    /// don't clean up any more because in that case the driver takes care of
    /// this for us.
    /// </summary>
    public abstract class GraphicsObject {
        private bool released;
        private IGraphicsContext context;

        public GraphicsObject()
        {
            context = GraphicsContext.CurrentContext;
            if (context == null)
                throw new InvalidOperationException("No OpenGL context found " +
                    "for thread " + Thread.CurrentThread.ManagedThreadId);
        }

        ~GraphicsObject()
        {
            if (released)
                return;

            // we were collected from a different thread.  Can't clean up
            // outselves here, we have to notify the game to do that after
            // the next iteration
            if (!context.IsCurrent) {
                Game game = Game.ForContext(context);

                // game went away or never existed.  In this case we have lost
                // our chance to clean up.  Let's hope the application quits
                // soon.
                if (game == null) {
                    released = true;
                    return;
                }

                game.RegisterForDisposal(this);
            } else {
                ReleaseResourcesSafely();
            }
        }

        public bool Released {
            get { return released; }
        }

        /// <summary>
        /// Like ReleaseResources but will make sure that it marks the object
        /// as released later.  The idea is that if someone forgets to mark
        /// the object as released we will not end up in a loop of endless
        /// releases.
        /// </summary>
        public void ReleaseResourcesSafely()
        {
            if (released)
                return;
            ReleaseResources();
            released = true;
        }

        /// <summary>
        /// Override this to release resources managed via OpenGL.
        /// </summary>
        protected abstract void ReleaseResources();
    }
}


using KUnpack.EngineSharp;

namespace KUnpack.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ các thao tác animation với sprite
    /// </summary>
    public class SpriteAnimationHelper : IDisposable
    {
        private const int DefaultAnimationFps = 24;

        private KSprite? currentSprite;
        private string? currentSpriteTempFile;
        private int currentFrameIndex;
        private bool _disposed;

        public KSprite? CurrentSprite => currentSprite;
        public int CurrentFrameIndex => currentFrameIndex;

        public bool LoadSprite(byte[] data)
        {
            CleanupSprite();

            currentSpriteTempFile = Path.Combine(Path.GetTempPath(), $"temp_sprite_{Guid.NewGuid()}.spr");
            File.WriteAllBytes(currentSpriteTempFile, data);

            currentSprite = new KSprite();
            return currentSprite.Load(currentSpriteTempFile);
        }

        public (bool isAnimated, bool hasMultipleFrames, double fps) AnalyzeSprite()
        {
            if (currentSprite == null)
                return (false, false, DefaultAnimationFps);

            int frames = currentSprite.GetFrames();
            int interval = currentSprite.GetInterval();

            bool isAnimated = frames > 1 && interval > 1;
            bool hasMultipleFrames = frames > 1;
            double fps = interval > 1 ? Math.Clamp(1000.0 / interval, 1.0, 120.0) : DefaultAnimationFps;

            return (isAnimated, hasMultipleFrames, fps);
        }

        public Bitmap? RenderFrame(int frameIndex)
        {
            if (currentSprite == null)
                return null;

            return currentSprite.RenderFrame(frameIndex);
        }

        public Bitmap? RenderSpriteSheet()
        {
            if (currentSprite == null)
                return null;

            return currentSprite.RenderSpriteSheet();
        }

        public void AdvanceFrame()
        {
            if (currentSprite == null)
                return;

            currentFrameIndex++;
            if (currentFrameIndex >= currentSprite.GetFrames())
                currentFrameIndex = 0;
        }

        public void ResetFrameIndex()
        {
            currentFrameIndex = 0;
        }

        public void CleanupSprite()
        {
            if (currentSprite != null)
            {
                currentSprite.Dispose();
                currentSprite = null;
            }

            if (currentSpriteTempFile != null)
            {
                try
                {
                    if (File.Exists(currentSpriteTempFile))
                        File.Delete(currentSpriteTempFile);
                }
                catch { }
                currentSpriteTempFile = null;
            }

            currentFrameIndex = 0;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanupSprite();
                }
                _disposed = true;
            }
        }
    }
}
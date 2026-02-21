using PhotonUI.Models.Properties;
using SDL3;

namespace PhotonUI.Models
{
    public readonly record struct FontMetrics
    {
        public string FontFamily { get; init; } = string.Empty;
        public string FontStyle { get; init; } = string.Empty;
        public int FontSize { get; init; } = -1;
        public int TextOutlineSize { get; init; } = 0;

        public int FontHeight { get; init; } = -1;
        public int FontAscent { get; init; } = -1;
        public int FontDescent { get; init; } = -1;
        public int FontLineSkip { get; init; } = -1;

        public FontMetrics(IntPtr font, TextProperties props)
        {
            if (font != IntPtr.Zero)
            {
                int height = TTF.GetFontHeight(font);
                int ascent = TTF.GetFontAscent(font);
                int descent = TTF.GetFontDescent(font);
                int lineSkip = TTF.GetFontLineSkip(font);

                if (props.TextOutlineSize > 0)
                {
                    height += props.TextOutlineSize * 2;
                    ascent += props.TextOutlineSize;
                    descent += props.TextOutlineSize;
                    lineSkip += props.TextOutlineSize * 2 + Math.Abs(descent);
                }

                this.FontHeight = height;
                this.FontAscent = ascent;
                this.FontDescent = descent;
                this.FontLineSkip = lineSkip;

                this.TextOutlineSize = props.TextOutlineSize;

                this.FontFamily = props.FontFamily;
                this.FontStyle = props.FontStyle;
                this.FontSize = props.FontSize;
            }
        }

        public Size MeasureString(IntPtr font, string text, uint length = 0)
        {
            if (font == IntPtr.Zero || string.IsNullOrEmpty(text))
                return Size.Empty;

            TTF.GetStringSize(font, text, length, out int w, out int h);

            int outline = Math.Max(0, this.TextOutlineSize);
            int padX = (int)Math.Ceiling(outline * 2.0);
            int padY = (int)Math.Ceiling(outline * 2.0) + Math.Abs(this.FontDescent);

            return new Size(w + padX, h + padY);
        }
        public bool MeasureString(IntPtr font, string text, uint length, int maxWidth, out int measuredWidth, out ulong measuredLength)
        {
            measuredWidth = 0;
            measuredLength = 0;

            if (font == IntPtr.Zero || string.IsNullOrEmpty(text))
                return false;

            if (TTF.MeasureString(font, text, length, maxWidth, out int rawWidth, out ulong rawLength))
            {
                measuredWidth = rawWidth + (this.TextOutlineSize * 2);
                measuredLength = rawLength + (uint)Math.Abs(this.FontDescent);
                return true;
            }

            return false;
        }
        public Size MeasureStringWrapped(IntPtr font, string text, uint length = 0, int wrapLength = 0)
        {
            if (font == IntPtr.Zero || string.IsNullOrEmpty(text))
                return Size.Empty;

            TTF.GetStringSizeWrapped(font, text, length, wrapLength, out int w, out int h);

            return new(w + this.TextOutlineSize * 2, h + this.TextOutlineSize * 2 + Math.Abs(this.FontDescent));
        }
        public Size MeasureText(IntPtr text)
        {
            if (text == IntPtr.Zero)
                return Size.Empty;

            TTF.GetTextSize(text, out int w, out int h);

            return new(w + this.TextOutlineSize * 2, h + this.TextOutlineSize * 2);
        }

        public float GlyphOffset(IntPtr font, char glyph)
        {
            if (font == IntPtr.Zero)
                throw new ArgumentException("Font handle is invalid.", nameof(font));

            if (TTF.GetGlyphMetrics(font, glyph, out int minx, out int maxx, out int _, out int _, out int advance))
            {
                float glyphVisualWidth = (maxx - minx) + (this.TextOutlineSize * 2);
                float glyphAdvance = advance;

                return (glyphVisualWidth - glyphAdvance) * 0.5f;
            }

            return 0f;
        }

        public int SkipLine => this.FontLineSkip > 0 ? this.FontLineSkip : this.FontHeight;
    }
}
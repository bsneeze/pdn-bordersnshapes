using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace pyrochild.effects.bordersnshapes
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class BordersNShapes
        : PropertyBasedEffect
    {
        float StartSweep;
        float EndSweep;
        float Spacing;
        Shape Shape;
        DashPattern DashPattern;
        DashCap DashCaps;
        LineCap EndCaps;
        int Width;
        bool AAMode;
        Color Color;

        public enum PropertyNames
        {
            StartSweep,
            EndSweep,
            Spacing,
            Shape,
            DashPattern,
            DashCaps,
            EndCaps,
            Width,
            AAMode,
            Color
        }

        public static string StaticName
        {
            get
            {
                string name = "Borders N' Shapes";
#if DEBUG
                name += " BETA";
#endif
                return name;
            }
        }

        public static string StaticDialogName { get { return StaticName + " by pyrochild"; } }

        public static Bitmap StaticIcon
        {
            get
            {
                return new Bitmap(typeof(BordersNShapes), "icon.png");
            }
        }

        public static string StaticSubMenuName
        {
            get
            {
                return SubmenuNames.Render;
            }
        }

        public BordersNShapes()
            : base(StaticName, StaticIcon, StaticSubMenuName, EffectFlags.Configurable)
        {
        }
        

        private float[] Pattern(DashPattern DashPattern, float Space)
        {
            if (Space == 0) Space = float.Epsilon;

            switch (DashPattern)
            {
                case DashPattern.Dotted:
                    return new float[] { 1f, Space };
                case DashPattern.Dashed:
                    return new float[] { 3f, Space };
                case DashPattern.DashDot:
                    return new float[] { 3f, Space, 1f, Space };
                case DashPattern.DashDotDot:
                    return new float[] { 3f, Space, 1f, Space, 1f, Space };
                case DashPattern.DashDashDot:
                    return new float[] { 3f, Space, 3f, Space, 1f, Space };
                case DashPattern.DashDashDotDot:
                    return new float[] { 3f, Space, 3f, Space, 1f, Space, 1f, Space };
                default:
                    return null;
            }
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new StaticListChoiceProperty(PropertyNames.Shape, EnumValues<Shape>()));
            props.Add(new Int32Property(PropertyNames.Width, 2, 1, 100));
            props.Add(new Int32Property(PropertyNames.Color, ColorBgra.ToOpaqueInt32(EnvironmentParameters.PrimaryColor.NewAlpha(255)), 0, 0xFFFFFF));
            props.Add(new BooleanProperty(PropertyNames.AAMode, true));
            props.Add(new StaticListChoiceProperty(PropertyNames.DashPattern, EnumValues<DashPattern>()));
            props.Add(new StaticListChoiceProperty(PropertyNames.DashCaps, EnumValues<DashCap>(), 0, true));
            props.Add(new DoubleProperty(PropertyNames.Spacing, 1, 0, 10, true));
            props.Add(new DoubleProperty(PropertyNames.StartSweep, 0, 0, 360, true));
            props.Add(new DoubleProperty(PropertyNames.EndSweep, 90, 0, 360, true));
            props.Add(new StaticListChoiceProperty(PropertyNames.EndCaps, new object[]{
                LineCap.NoAnchor,
                LineCap.Flat,
                LineCap.Round,
                LineCap.Triangle,
                LineCap.SquareAnchor,
                LineCap.RoundAnchor,
                LineCap.DiamondAnchor,
                LineCap.ArrowAnchor
                }, 0, true));

            List<PropertyCollectionRule> rules = new List<PropertyCollectionRule>();

            rules.Add(new ReadOnlyBoundToNameValuesRule(PropertyNames.DashCaps, false,
                TupleStruct.Create<object, object>(PropertyNames.DashPattern, DashPattern.Solid)));

            rules.Add(new ReadOnlyBoundToNameValuesRule(PropertyNames.Spacing, false,
                TupleStruct.Create<object, object>(PropertyNames.DashPattern, DashPattern.Solid)));

            rules.Add(new ReadOnlyBoundToNameValuesRule(PropertyNames.EndCaps, true,
                TupleStruct.Create<object, object>(PropertyNames.Shape, Shape.Arc)));

            rules.Add(new ReadOnlyBoundToNameValuesRule(PropertyNames.StartSweep, true,
                TupleStruct.Create<object, object>(PropertyNames.Shape, Shape.Arc),
                TupleStruct.Create<object, object>(PropertyNames.Shape, Shape.Pie)));

            rules.Add(new ReadOnlyBoundToNameValuesRule(PropertyNames.EndSweep, true,
                TupleStruct.Create<object, object>(PropertyNames.Shape, Shape.Arc),
                TupleStruct.Create<object, object>(PropertyNames.Shape, Shape.Pie)));

            return new PropertyCollection(props, rules);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlType(PropertyNames.Color, PropertyControlType.ColorWheel);
            configUI.SetPropertyControlType(PropertyNames.StartSweep, PropertyControlType.AngleChooser);
            configUI.SetPropertyControlType(PropertyNames.EndSweep, PropertyControlType.AngleChooser);
            configUI.SetPropertyControlValue(PropertyNames.AAMode, ControlInfoPropertyNames.Description, "Anti-alias");
            configUI.SetPropertyControlValue(PropertyNames.AAMode, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.DashCaps, ControlInfoPropertyNames.DisplayName, "Pattern Caps");
            configUI.SetPropertyControlValue(PropertyNames.DashPattern, ControlInfoPropertyNames.DisplayName, "Pattern");
            configUI.SetPropertyControlValue(PropertyNames.EndCaps, ControlInfoPropertyNames.DisplayName, "Line Caps");
            configUI.SetPropertyControlValue(PropertyNames.EndSweep, ControlInfoPropertyNames.DisplayName, "");
            configUI.SetPropertyControlValue(PropertyNames.StartSweep, ControlInfoPropertyNames.DisplayName, "Sweep");
            configUI.SetPropertyControlValue(PropertyNames.Spacing, ControlInfoPropertyNames.UpDownIncrement, .1);

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            props[ControlInfoPropertyNames.WindowTitle].Value = StaticDialogName;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            this.AAMode = newToken.GetProperty<BooleanProperty>(PropertyNames.AAMode).Value;
            this.Color = Color.FromArgb(255, Color.FromArgb(newToken.GetProperty<Int32Property>(PropertyNames.Color).Value));
            this.DashCaps = (DashCap)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.DashCaps).Value;
            this.DashPattern = (DashPattern)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.DashPattern).Value;
            this.EndCaps = (LineCap)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.EndCaps).Value;
            this.EndSweep = (float)newToken.GetProperty<DoubleProperty>(PropertyNames.EndSweep).Value;
            this.Shape = (Shape)newToken.GetProperty<StaticListChoiceProperty>(PropertyNames.Shape).Value;
            this.Spacing = (float)newToken.GetProperty<DoubleProperty>(PropertyNames.Spacing).Value;
            this.StartSweep = (float)newToken.GetProperty<DoubleProperty>(PropertyNames.StartSweep).Value;
            this.Width = newToken.GetProperty<Int32Property>(PropertyNames.Width).Value;
            
            PdnRegion selection = EnvironmentParameters.GetSelection(srcArgs.Bounds);
            Rectangle rselection = selection.GetBoundsInt();
            RectangleF rdraw = new Rectangle(0, 0, rselection.Width, rselection.Height);

            Region clipregion = new Region(selection.GetRegionData());
            clipregion.Translate(-rselection.X, -rselection.Y);
            dstArgs.Graphics.Clip = clipregion;

            dstArgs.Surface.CopySurface(SrcArgs.Surface);

            if (StartSweep > EndSweep)
            {
                var temp = StartSweep;
                StartSweep = EndSweep;
                EndSweep = temp;
            }

            if (Shape != Shape.Arc)
            {
                EndCaps = LineCap.NoAnchor;
            }

            using (Pen p = new Pen(Color, (float)Width))
            {
                p.DashCap = DashCaps;
                p.StartCap = EndCaps;
                p.EndCap = EndCaps;
                p.LineJoin = LineJoin.Miter; // this could use a setting in the UI...

                // bring the rectangle in or half our line width will be outside the canvas/selection
                rdraw.X += (Width / 2f);
                rdraw.Y += (Width / 2f);
                rdraw.Width -= Width;
                rdraw.Height -= Width;

                // bring it in more with "anchor" style line caps which are wider than the main line
                if (EndCaps == LineCap.ArrowAnchor ||
                    EndCaps == LineCap.DiamondAnchor ||
                    EndCaps == LineCap.RoundAnchor ||
                    EndCaps == LineCap.SquareAnchor)
                {
                    rdraw.X += (Width / 2f);
                    rdraw.Y += (Width / 2f);
                    rdraw.Width -= Width;
                    rdraw.Height -= Width;
                }

                dstArgs.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                if (AAMode)
                {
                    dstArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                }
                else
                {
                    dstArgs.Graphics.SmoothingMode = SmoothingMode.None;
                }

                if (DashPattern != DashPattern.Solid)
                {
                    p.DashPattern = Pattern(DashPattern, Spacing);
                }
                else
                {
                    p.DashStyle = DashStyle.Solid;
                }

                switch (Shape)
                {
                    case Shape.Arc:
                        dstArgs.Graphics.DrawArc(p, rdraw, -StartSweep, StartSweep - EndSweep);
                        break;

                    case Shape.Ellipse:
                        dstArgs.Graphics.DrawEllipse(p, rdraw);
                        break;

                    case Shape.Pie:
                        dstArgs.Graphics.DrawPie(p, rdraw, -StartSweep, StartSweep - EndSweep);
                        break;

                    case Shape.Rectangle:
                        dstArgs.Graphics.DrawRectangle(p, rdraw.X, rdraw.Y, rdraw.Width, rdraw.Height);
                        break;
                }
            }

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            // render in OnSetRenderInfo
        }

        /// <summary>
        /// Gets all of the values of an enum
        /// </summary>
        /// <typeparam name="T">enum's type</typeparam>
        /// <returns>object[] of enum values</returns>
        private object[] EnumValues<T>()
        {
            if (!typeof(T).IsEnum) throw new ArgumentException();
            var values = Enum.GetValues(typeof(T));
            var retval = new object[values.Length];
            values.CopyTo(retval, 0);
            return retval;
        }
    }
}
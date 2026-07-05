using System.Collections.Generic;
using Bitmute.Tools;

namespace Bitmute.UI
{
	public class ToolBox
	{
		private ToolState m_toolState;
		private Dictionary<eTool, Tool> m_tools;
		private FreeTransformTool m_freeTransformTool;
		private eTool m_previousTool;

		public ToolBox()
		{
			m_toolState = new ToolState();
			m_freeTransformTool = new FreeTransformTool();
			m_previousTool = eTool.Brush;
			m_tools = new Dictionary<eTool, Tool>();
			m_tools.Add(eTool.Move, new MoveTool());
			m_tools.Add(eTool.Select, new RectangleSelectTool());
			m_tools.Add(eTool.EllipseSelect, new EllipseSelectTool());
			m_tools.Add(eTool.Lasso, new LassoTool());
			m_tools.Add(eTool.FreehandLasso, new FreehandLassoTool());
			m_tools.Add(eTool.MagneticLasso, new MagneticLassoTool());
			m_tools.Add(eTool.MagicWand, new MagicWandTool());
			m_tools.Add(eTool.Text, new TextTool());
			m_tools.Add(eTool.Pencil, new PencilTool());
			m_tools.Add(eTool.Brush, new BrushTool());
			m_tools.Add(eTool.Eraser, new EraserTool());
			m_tools.Add(eTool.DodgeBurn, new DodgeBurnTool());
			m_tools.Add(eTool.Blur, new BlurTool());
			m_tools.Add(eTool.Sponge, new SpongeTool());
			m_tools.Add(eTool.ColorReplacement, new ColorReplacementTool());
			m_tools.Add(eTool.Sharpen, new SharpenTool());
			m_tools.Add(eTool.Clone, new CloneTool());
			m_tools.Add(eTool.Heal, new HealTool());
			m_tools.Add(eTool.Smudge, new SmudgeTool());
			m_tools.Add(eTool.Eyedropper, new EyedropperTool());
			m_tools.Add(eTool.Fill, new FillTool());
			m_tools.Add(eTool.Gradient, new GradientTool());
			m_tools.Add(eTool.Line, new LineTool());
			m_tools.Add(eTool.RectangleShape, new ShapeTool(eShapeKind.Rectangle));
			m_tools.Add(eTool.RoundedRectangleShape, new ShapeTool(eShapeKind.RoundedRectangle));
			m_tools.Add(eTool.EllipseShape, new ShapeTool(eShapeKind.Ellipse));
			m_tools.Add(eTool.PolygonShape, new ShapeTool(eShapeKind.Polygon));
			m_tools.Add(eTool.Hand, new HandTool());
			m_tools.Add(eTool.Zoom, new ZoomTool());
			m_tools.Add(eTool.Ruler, new RulerTool());
			m_tools.Add(eTool.Crop, new CropTool());
			m_tools.Add(eTool.FreeTransform, m_freeTransformTool);
		}

		public ToolState State()
		{
			return m_toolState;
		}

		public Tool Instance(eTool tool)
		{
			Tool instance;
			bool found = m_tools.TryGetValue(tool, out instance);
			if (found)
			{
				return instance;
			}
			return null;
		}

		public FreeTransformTool FreeTransform()
		{
			return m_freeTransformTool;
		}

		public CropTool Crop()
		{
			return (CropTool)m_tools[eTool.Crop];
		}

		public LassoTool Lasso()
		{
			return (LassoTool)m_tools[eTool.Lasso];
		}

		public eTool PreviousTool()
		{
			return m_previousTool;
		}

		public void SetPreviousTool(eTool tool)
		{
			m_previousTool = tool;
		}

		public void ResetAll()
		{
			foreach (Tool tool in m_tools.Values)
			{
				tool.Reset();
			}
		}
	}
}

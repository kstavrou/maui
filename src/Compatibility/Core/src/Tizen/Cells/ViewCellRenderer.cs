using System.Collections.Generic;
using ElmSharp;

namespace Microsoft.Maui.Controls.Compatibility.Platform.Tizen
{
	[System.Obsolete(Compatibility.Hosting.MauiAppBuilderExtensions.UseMapperInstead)]
	public class ViewCellRenderer : CellRenderer
	{
		readonly Dictionary<EvasObject, ViewCell> _cacheCandidate = new Dictionary<EvasObject, ViewCell>();
		public ViewCellRenderer() : base(ThemeManager.GetViewCellRendererStyle())
		{
			MainContentPart = this.GetMainContentPart();
		}

		protected string MainContentPart { get; set; }

		protected override EvasObject OnReusableContent(Cell cell, string part, EvasObject old)
		{
			if (_cacheCandidate.ContainsKey(old))
			{
				var viewCell = _cacheCandidate[old];
				var widget = (old as Widget);
				if (widget != null)
					widget.IsEnabled = true;
				viewCell.BindingContext = cell.BindingContext;
				return old;
			}
			return null;
		}

		protected override EvasObject OnGetContent(Cell cell, string part)
		{
			if (part == MainContentPart)
			{
				var viewCell = (ViewCell)cell;

				var listView = viewCell?.RealParent as ListView;

				// It is a condition for reusable the cell
				if (listView != null &&
					listView.HasUnevenRows == false &&
					!(listView.ItemTemplate is DataTemplateSelector) && !GetCurrentItem().IsGroupItem)
				{
					return CreateReusableContent(viewCell);
				}

				Platform.GetRenderer(viewCell.View)?.Dispose();
				var renderer = Platform.GetOrCreateRenderer(viewCell.View);
				double height = viewCell.RenderHeight;
				height = height > 0 ? height : FindCellContentHeight(viewCell);

				renderer.NativeView.MinimumHeight = Forms.ConvertToScaledPixel(height);
				(renderer as ILayoutRenderer)?.RegisterOnLayoutUpdated();

				UpdatePropagateEvent(viewCell.View);

				return renderer.NativeView;
			}
			return null;
		}

		protected override bool OnCellPropertyChanged(Cell cell, string property, Dictionary<string, EvasObject> realizedView)
		{
			if (property == "View")
			{
				return true;
			}
			return base.OnCellPropertyChanged(cell, property, realizedView);
		}

		EvasObject CreateReusableContent(ViewCell viewCell)
		{
			var listView = viewCell.RealParent as ListView;
			ViewCell duplicatedCell = (ViewCell)listView.ItemTemplate.CreateContent();
			duplicatedCell.BindingContext = viewCell.BindingContext;
			duplicatedCell.Parent = listView;

			var renderer = Platform.GetOrCreateRenderer(duplicatedCell.View);
			double height = duplicatedCell.RenderHeight;
			height = height > 0 ? height : FindCellContentHeight(duplicatedCell);
			renderer.NativeView.MinimumHeight = Forms.ConvertToScaledPixel(height);

			_cacheCandidate[renderer.NativeView] = duplicatedCell;
			renderer.NativeView.Deleted += (sender, e) =>
			{
				_cacheCandidate.Remove((EvasObject)sender);
			};
			(renderer as ILayoutRenderer)?.RegisterOnLayoutUpdated();

			UpdatePropagateEvent(duplicatedCell.View);
			return renderer.NativeView;
		}

		void UpdatePropagateEvent(View view)
		{
			if (!view.IsPlatformEnabled)
				return;
			foreach (var element in view.Descendants())
			{
				if (element is Button || element is Switch)
				{
					var nativeView = Platform.GetRenderer(element)?.NativeView ?? null;
					if (nativeView != null)
					{
						nativeView.PropagateEvents = false;
					}
				}
			}
		}

	}
}

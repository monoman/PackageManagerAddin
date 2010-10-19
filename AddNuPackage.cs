using System;

namespace NuPackAddin
{
	public partial class AddNuPackage : Gtk.Dialog
	{
		public AddNuPackage ()
		{
			this.Build ();
		}
		
		protected virtual void OnButtonCloseClicked (object sender, System.EventArgs e)
		{
			this.Destroy ();
		}

		protected virtual void OnButtonSettingsClicked (object sender, System.EventArgs e)
		{
			// TODO: Show configuration dialog
		}
	}
}


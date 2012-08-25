// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace Reachability
{
	[Register ("ReachabilityViewController")]
	partial class ReachabilityViewController
	{
		[Outlet]
		MonoTouch.UIKit.UILabel StatusLabel { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel WifiStatusLabel { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel InternetStatusLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (StatusLabel != null) {
				StatusLabel.Dispose ();
				StatusLabel = null;
			}

			if (WifiStatusLabel != null) {
				WifiStatusLabel.Dispose ();
				WifiStatusLabel = null;
			}

			if (InternetStatusLabel != null) {
				InternetStatusLabel.Dispose ();
				InternetStatusLabel = null;
			}
		}
	}
}

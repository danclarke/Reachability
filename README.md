#Reachability

MonoTouch port of Reachability by Tony Million: http://www.tonymillion.com/post/12736685700/im-back-baby

Allows for you to easily check for network connectivity (and what type), as well as events to notify you as the connection changes.

More information can be found here: http://dan.clarke.name/2012/08/reachability-in-monotouch/

##Usage

	private Reachability _reachability;
	
	public override void ViewDidLoad()
	{
		base.ViewDidLoad();
		
		_reachability = new Reachability("www.google.co.uk");
		_reachability.ReachabilityUpdated += HandleReachabilityUpdated;
	}
	
	protected virtual void HandleReachabilityUpdated(object sender, ReachabilityEventArgs e)
	{
		UpdateStatusLabel(e.Status, StatusLabel);
	}
	
	protected virtual void UpdateStatusLabel(ReachabilityStatus status, UILabel label)
	{
		switch (status)
		{
			case ReachabilityStatus.NotReachable:
				label.Text = "Not Reachable";
				break;
	
			case ReachabilityStatus.ViaWiFi:
				label.Text = "Via WiFi";
				break;
	
			case ReachabilityStatus.ViaWWAN:
				label.Text = "Via WWAN";
				break;
	
			default:
				label.Text = "Unexpected status";
				break;
		}
	}

##License

Licensed under the BSD license:

Copyright (c) 2012, Dan Clarke
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
* Neither the name of Dan Clarke nor the names of contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
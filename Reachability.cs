/*
    Copyright (c) 2012, Dan Clarke
    All rights reserved.

    Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

        Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
        Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
        Neither the name of Dan Clarke nor the names of contributors may be used to endorse or promote products derived from this software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Net;

#if !MONOMAC
    using MonoTouch.Foundation;
    using MonoTouch.SystemConfiguration;
    using MonoTouch.CoreFoundation;
#else
    using MonoMac.Foundation;
    using MonoMac.CoreFoundation;
#endif

namespace Reachability
{
    /// <summary>
    /// Class for checking network reachability via WiFi / WWAN (Cellular)
    /// </summary>
    public class Reachability : IDisposable
    {
        // iOS apps are most likely to have a WWAN interface, MonoMac apps, probably not
#if !MONOMAC
        protected const bool DefaultHasWWAN = true;
#else
        protected const bool DefaultHasWWAN = false;
#endif

        /// <summary>
        /// Hardcoded copy of IN_LINKLOCALNETNUM in <netinet/in.h>
        /// </summary>
        protected static readonly byte[] LocalNetNum = { 169, 254, 0, 0};

        /// <summary>
        /// The NetworkReachability instance to work on
        /// </summary>
        /// <value>
        /// The network reachability.
        /// </value>
        protected NetworkReachability NetworkReachability { get; set; }

        /// <summary>
        /// Whether the platform has WWAN
        /// </summary>
        protected bool HasWWAN { get; set; }

        /// <summary>
        /// The reachability status has changed
        /// </summary>
        public event EventHandler<ReachabilityEventArgs> ReachabilityUpdated;

        /// <summary>
        /// Set whether WWAN counts as 'connected'. Default is True.
        /// </summary>
        public bool AllowWWAN { get; set; }

        private bool _disposed;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Reachability.Reachability"/> class.
        /// </summary>
        /// <param name='reachability'>
        /// NetworkReachability instance to use
        /// </param>
        /// <param name='hasWWAN'>
        /// Platform has a WWAN interface
        /// </param>
        protected Reachability(NetworkReachability reachability, bool hasWWAN)
        {
            NetworkReachability = reachability;
            HasWWAN = hasWWAN;
            AllowWWAN = true;
            NetworkReachability.SetCallback(OnReachabilityNotification);
            NetworkReachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Reachability.Reachability"/> class.
        /// </summary>
        /// <param name='hostname'>
        /// Target hostname
        /// </param>
        /// <param name='hasWWAN'>
        /// Set whether the platform has a WWAN interface
        /// </param>
        public Reachability(string hostname, bool hasWWAN = DefaultHasWWAN) 
            : this(new NetworkReachability(hostname), hasWWAN) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Reachability.Reachability"/> class.
        /// </summary>
        /// <param name='address'>
        /// Target IP address
        /// </param>
        /// <param name='hasWWAN'>
        /// Set whether the platform has a WWAN interface
        /// </param>
        public Reachability(IPAddress address, bool hasWWAN = DefaultHasWWAN) 
            : this(new NetworkReachability(address), hasWWAN) {}

        /// <summary>
        /// Get a new <see cref="Reachability.Reachability"/> instance that checks for general Internet access
        /// </summary>
        /// <returns>
        /// <see cref="Reachability.Reachability"/> instance
        /// </returns>
        /// <param name='hasWWAN'>
        /// Set whether the platform has a WWAN interface
        /// </param>
        public static Reachability ReachabilityForInternet(bool hasWWAN = DefaultHasWWAN)
        {
            return new Reachability(new IPAddress(0), hasWWAN);
        }

        /// <summary>
        /// Get a new <see cref="Reachability.Reachability"/> instance that checks for local network access via WiFi
        /// </summary>
        /// <returns>
        /// <see cref="Reachability.Reachability"/> instance
        /// </returns>
        /// <param name='hasWWAN'>
        /// Set whether the platform has a WWAN interface
        /// </param>
        public static Reachability ReachabilityForLocalWiFi(bool hasWWAN = DefaultHasWWAN)
        {
            return new Reachability(new IPAddress(LocalNetNum), hasWWAN);
        }

        #endregion

        #region Flag util

        /// <summary>
        /// Get whether the requested resource is reachable with the specified flags
        /// </summary>
        /// <returns>
        /// <c>true</c> if the requested resource is reachable with the specified flags; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='flags'>
        /// Reachability flags.
        /// </param>
        /// <remarks>
        /// This is for the case where you flick the airplane mode you end up getting something like this:
        /// Reachability: WR ct-----
        /// Reachability: -- -------
        /// Reachability: WR ct-----
        /// Reachability: -- -------
        /// We treat this as 4 UNREACHABLE triggers - really Apple should do better than this
        /// </remarks>
        private bool IsReachableWithFlags(NetworkReachabilityFlags flags)
        {
            if (!flags.HasFlag(NetworkReachabilityFlags.Reachable))
                return false;

            var testCase = NetworkReachabilityFlags.ConnectionRequired | NetworkReachabilityFlags.TransientConnection;
            if ((flags & testCase) == testCase)
                return false;

            if (HasWWAN)
            {
                // If we're connecting via WWAN, and WWAN doesn't count as connected, return false
                if (flags.HasFlag(NetworkReachabilityFlags.IsWWAN) && !AllowWWAN)
                    return false;
            }

            return true;
        }

        private bool ReachableViaWWAN(NetworkReachabilityFlags flags)
        {
            if (!HasWWAN)
                return false;

            return (flags.HasFlag(NetworkReachabilityFlags.Reachable) && flags.HasFlag(NetworkReachabilityFlags.IsWWAN));
        }

        private bool ReachableViaWiFi(NetworkReachabilityFlags flags)
        {
            if (!flags.HasFlag(NetworkReachabilityFlags.Reachable))
                return false;

            // If we don't have WWAN, reachable = WiFi (close enough)
            if (!HasWWAN)
                return true;

            // We have WWAN, if we're connecting through WWAN, we're not connecting through WiFi
            if (flags.HasFlag(NetworkReachabilityFlags.IsWWAN))
                return false;
            else
                return true; // Connecting through WiFi, not WWAN
        }

        /// <summary>
        /// Utility method that gets the flags as a string
        /// </summary>
        /// <returns>
        /// The flags as string
        /// </returns>
        /// <param name='flags'>
        /// Flags
        /// </param>
        protected string GetFlagsAsString(NetworkReachabilityFlags flags)
        {
            return string.Concat(
                !HasWWAN ? "X" : flags.HasFlag(NetworkReachabilityFlags.IsWWAN) ? "W" : "-",
                flags.HasFlag(NetworkReachabilityFlags.Reachable) ?               "R" : "-",
                                                                                  " ",
                flags.HasFlag(NetworkReachabilityFlags.ConnectionRequired) ?      "c" : "-",
                flags.HasFlag(NetworkReachabilityFlags.TransientConnection) ?     "t" : "-",
                flags.HasFlag(NetworkReachabilityFlags.InterventionRequired) ?    "i" : "-",
                flags.HasFlag(NetworkReachabilityFlags.ConnectionOnTraffic) ?     "C" : "-",
                flags.HasFlag(NetworkReachabilityFlags.ConnectionOnDemand) ?      "D" : "-",
                flags.HasFlag(NetworkReachabilityFlags.IsLocalAddress) ?          "l" : "-",
                flags.HasFlag(NetworkReachabilityFlags.IsDirect) ?                "d" : "-"
                );
        }

        #endregion

        /// <summary>
        /// Get the current status as an English string
        /// </summary>
        /// <returns>
        /// The status string
        /// </returns>
        public virtual string GetReachabilityString()
        {
            switch (CurrentStatus)
            {
                case ReachabilityStatus.ViaWWAN:
                    return "Cellular";

                case ReachabilityStatus.ViaWiFi:
                    return "WiFi";

                default:
                    return "No Connection";
            }
        }

        #region Notifications

        protected virtual void OnReachabilityNotification(NetworkReachabilityFlags flags)
        {
            if (ReachabilityUpdated == null)
                return;

            ReachabilityStatus status = ReachabilityStatus.NotReachable;

            if (!IsReachableWithFlags(flags))
                status = ReachabilityStatus.NotReachable;
            else if (ReachableViaWWAN(flags))
                status = ReachabilityStatus.ViaWWAN;
            else if (ReachableViaWiFi(flags))
                status = ReachabilityStatus.ViaWiFi;

            if (ReachabilityUpdated != null)
                ReachabilityUpdated(this, new ReachabilityEventArgs(status));
        }

        #endregion

        #region Status properties

        /// <summary>
        /// Requested resource is reachable
        /// </summary>
        public virtual bool IsReachable
        {
            get
            {
                NetworkReachabilityFlags flags;

                if (!NetworkReachability.TryGetFlags(out flags))
                    return false;

                return IsReachableWithFlags(flags);
            }
        }

        /// <summary>
        /// Requested resource is reachable via WWAN
        /// </summary>
        public virtual bool IsReachableViaWWAN
        {
            get
            {
                if (!HasWWAN)
                    return false;

                NetworkReachabilityFlags flags;

                if (!NetworkReachability.TryGetFlags(out flags))
                    return false;

                return ReachableViaWWAN(flags);
            }
        }

        /// <summary>
        /// Requested resource is reachable via WiFi
        /// </summary>
        public virtual bool IsReachableViaWiFi
        {
            get
            {
                NetworkReachabilityFlags flags;

                if (!NetworkReachability.TryGetFlags(out flags))
                    return false;

                if (!flags.HasFlag(NetworkReachabilityFlags.Reachable))
                    return false;

                return ReachableViaWiFi(flags);
            }
        }

        /// <summary>
        /// WWAN is not active until a connection is required
        /// </summary>
        public virtual bool IsConnectionRequired
        {
            get
            {
                NetworkReachabilityFlags flags;

                if (!NetworkReachability.TryGetFlags(out flags))
                    return false;

                return flags.HasFlag(NetworkReachabilityFlags.ConnectionRequired);
            }
        }

        /// <summary>
        /// Connection will be automatically made, but only on demand. The link is otherwise inactive.
        /// </summary>
        public virtual bool IsConnectionOnDemand
        {
            get
            {
                NetworkReachabilityFlags flags;

                if (!NetworkReachability.TryGetFlags(out flags))
                    return false;

                return (flags.HasFlag(NetworkReachabilityFlags.ConnectionRequired) &&
                        flags.HasFlag(NetworkReachabilityFlags.ConnectionOnTraffic) &&
                        flags.HasFlag(NetworkReachabilityFlags.ConnectionOnDemand));
            }
        }

        /// <summary>
        /// The requested resource is reachable, but it'll require user interaction
        /// </summary>
        public virtual bool IsInterventionRequired
        {
            get
            {
                NetworkReachabilityFlags flags;

                if (!NetworkReachability.TryGetFlags(out flags))
                    return false;

                return (flags.HasFlag(NetworkReachabilityFlags.ConnectionRequired) &&
                        flags.HasFlag(NetworkReachabilityFlags.InterventionRequired));
            }
        }

        /// <summary>
        /// Gets the current reachability status
        /// </summary>
        /// <value>
        /// The current status
        /// </value>
        public virtual ReachabilityStatus CurrentStatus
        {
            get
            {
                if (!IsReachable)
                    return ReachabilityStatus.NotReachable;

                if (IsReachableViaWiFi)
                    return ReachabilityStatus.ViaWiFi;

                if (IsReachableViaWWAN)
                    return ReachabilityStatus.ViaWWAN;

                return ReachabilityStatus.NotReachable;
            }
        }

        #endregion

        #region IDisposable implementation

        ~Reachability()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            NetworkReachability.SetCallback(null);
            NetworkReachability.Dispose();
            NetworkReachability = null;

            _disposed = true;
        }

        #endregion

    }

    /// <summary>
    /// Reachability status.
    /// </summary>
    public enum ReachabilityStatus
    {
        /// <summary>
        /// Resource is not reachable.
        /// </summary>
        NotReachable = 0,

        /// <summary>
        /// Resource is reachable via WiFi.
        /// </summary>
        ViaWiFi,

        /// <summary>
        /// Resource is reachable via WWAN
        /// </summary>
        ViaWWAN
    }

    /// <summary>
    /// Event arguments that represent reachability
    /// </summary>
    public class ReachabilityEventArgs : EventArgs
    {
        private readonly ReachabilityStatus _status;

        /// <summary>
        /// Gets the reachability status.
        /// </summary>
        /// <value>
        /// The status
        /// </value>
        public ReachabilityStatus Status { get { return _status; } }

        public ReachabilityEventArgs(ReachabilityStatus status)
        {
            _status = status;
        }
    }
}


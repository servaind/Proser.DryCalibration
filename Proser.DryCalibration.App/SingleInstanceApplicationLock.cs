using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Proser.DryCalibration.App
{

    sealed class SingleInstanceApplicationLock : IDisposable
    {
        ~SingleInstanceApplicationLock()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool TryAcquireExclusiveLock()
        {
            try
            {
                if (!mutex.WaitOne(1000, false))
                    return false;
            }
            catch (AbandonedMutexException)
            {
            }

            return hasAcquiredExclusiveLock = true;
        }

        private const string MutexId = @"Local\{1109F104-B4B4-4ED1-920C-F4D8EFE9E833}";
        private readonly Mutex mutex = CreateMutex();
        private bool hasAcquiredExclusiveLock, disposed;

        private void Dispose(bool disposing)
        {
            if (disposing && !disposed && mutex != null)
            {
                try
                {
                    if (hasAcquiredExclusiveLock)
                        mutex.ReleaseMutex();

                    mutex.Dispose();
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        private static Mutex CreateMutex()
        {
            var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var allowEveryoneRule = new MutexAccessRule(sid,
                MutexRights.FullControl, AccessControlType.Allow);

            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);

            var mutex = new Mutex(false, MutexId);
            mutex.SetAccessControl(securitySettings);

            return mutex;
        }
    }

}

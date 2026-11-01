using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;

namespace MultiWallFX
{
    public static class LockScreenManager
    {
        public static bool SetLockScreen(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return false;

            // Method 1: WinRT LockScreen.SetImageFileAsync
            if (SetLockScreenViaWinRT(imagePath))
            {
                return true;
            }

            // Method 2: Registry Fallback
            return SetLockScreenViaRegistry(imagePath);
        }

        private static bool SetLockScreenViaWinRT(string imagePath)
        {
            try
            {
                Type tStorage = Type.GetType("Windows.Storage.StorageFile, Windows.Storage, ContentType=WindowsRuntime");
                Type tLock = Type.GetType("Windows.System.UserProfile.LockScreen, Windows.System, ContentType=WindowsRuntime");

                if (tStorage == null || tLock == null)
                    return false;

                MethodInfo mGetFile = tStorage.GetMethod("GetFileFromPathAsync", new Type[] { typeof(string) });
                if (mGetFile == null) return false;

                object asyncFileOp = mGetFile.Invoke(null, new object[] { Path.GetFullPath(imagePath) });
                if (asyncFileOp == null) return false;

                // Wait for file async op to complete
                object storageFile = WaitForAsyncResult(asyncFileOp, 5000);
                if (storageFile == null) return false;

                MethodInfo mSetLock = null;
                foreach (var m in tLock.GetMethods())
                {
                    if (m.Name == "SetImageFileAsync")
                    {
                        mSetLock = m;
                        break;
                    }
                }

                if (mSetLock == null) return false;

                object asyncSetAction = mSetLock.Invoke(null, new object[] { storageFile });
                if (asyncSetAction == null) return false;

                WaitForAsyncAction(asyncSetAction, 5000);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("WinRT LockScreen error: " + ex.Message);
                return false;
            }
        }

        private static object WaitForAsyncResult(object asyncOp, int timeoutMs)
        {
            PropertyInfo propStatus = asyncOp.GetType().GetProperty("Status");
            MethodInfo mGetResults = asyncOp.GetType().GetMethod("GetResults");

            int waited = 0;
            while (waited < timeoutMs)
            {
                if (propStatus != null)
                {
                    object status = propStatus.GetValue(asyncOp, null);
                    string statusStr = status != null ? status.ToString() : "";
                    if (statusStr == "Completed")
                    {
                        return mGetResults != null ? mGetResults.Invoke(asyncOp, null) : null;
                    }
                    if (statusStr == "Error" || statusStr == "Canceled")
                    {
                        return null;
                    }
                }
                Thread.Sleep(25);
                waited += 25;
            }
            return null;
        }

        private static void WaitForAsyncAction(object asyncAction, int timeoutMs)
        {
            PropertyInfo propStatus = asyncAction.GetType().GetProperty("Status");
            int waited = 0;
            while (waited < timeoutMs)
            {
                if (propStatus != null)
                {
                    object status = propStatus.GetValue(asyncAction, null);
                    string statusStr = status != null ? status.ToString() : "";
                    if (statusStr == "Completed" || statusStr == "Error" || statusStr == "Canceled")
                    {
                        return;
                    }
                }
                Thread.Sleep(25);
                waited += 25;
            }
        }

        private static bool SetLockScreenViaRegistry(string imagePath)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\PersonalizationModern"))
                {
                    if (key != null)
                    {
                        key.SetValue("LockScreenImage", imagePath, RegistryValueKind.String);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

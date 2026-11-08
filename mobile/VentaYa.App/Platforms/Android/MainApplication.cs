using Android.App;
using Android.Runtime;

namespace VentaYa.App;

// En desarrollo la API corre en http://10.0.2.2:5080 (sin https), por eso se permite tráfico sin cifrar SOLO en Debug.
// La versión Release usa https (MonsterASP) y no lo permite.
#if DEBUG
[Application(UsesCleartextTraffic = true)]
#else
[Application]
#endif
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

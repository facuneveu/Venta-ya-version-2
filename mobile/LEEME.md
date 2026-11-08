# App móvil VentaYa (.NET MAUI, Android)

| Proyecto | Qué contiene |
|---|---|
| `VentaYa.App.Core` | Lógica sin interfaz: cliente de la API, sesión y ViewModels de todas las pantallas. Probada con xUnit (`backend/VentaYa.Tests/App`) |
| `VentaYa.App` | Interfaz MAUI: 16 pantallas XAML, navegación con Shell, guardado seguro del token |

## Requisitos
- .NET SDK 10 y el workload `maui-android` (`dotnet workload install maui-android`, como administrador).
- Android SDK con la plataforma **API 36**. Si no la tenés: `dotnet build -t:InstallAndroidDependencies -f net10.0-android -p:AndroidSdkDirectory=<carpeta> -p:AcceptAndroidSDKLicenses=True`.
- `Directory.Build.props` usa `D:\android-sdk` si existe; si tu SDK está en otro lado, pasá `-p:AndroidSdkDirectory=...`.

## Compilar
```bash
cd mobile/VentaYa.App
dotnet build -f net10.0-android
```

## Generar un APK instalable
```bash
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk -p:RunAOTCompilation=false -o ../apk
```
El APK queda en `mobile/apk/com.ventaya.app-Signed.apk` (carpeta fuera de git). Copialo al celular e instalalo (hay que permitir "orígenes desconocidos").

## Conectar la app con la API
La dirección de la API se puede cambiar **sin recompilar**: en la pantalla de login tocá el texto "Servidor: …" al pie.

| Dónde corre la app | Dirección de la API |
|---|---|
| Emulador de Android (Debug, por defecto) | `http://10.0.2.2:5080/` |
| Celular real en la misma WiFi que tu PC | `http://<IP-de-tu-PC>:5080/` (arrancar la API con `dotnet run --urls http://0.0.0.0:5080`) |
| Servidor desplegado (MonsterASP) | `https://<tu-sitio>.monsterasp.net/` |

> La versión **Release** no permite tráfico `http` (solo `https`); para probar contra tu PC usá la versión Debug.

## Correr en un emulador o celular
```bash
dotnet build -f net10.0-android -t:Run
```
Con un celular: activá *Opciones de desarrollador → Depuración USB* y conectalo. Para un emulador hay que crear un AVD (Visual Studio → Herramientas → Android → Administrador de dispositivos).

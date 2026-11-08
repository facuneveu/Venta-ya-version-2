using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VentaYa.Api.Models;

namespace VentaYa.Api.Data;

public class VentaYaDbContext(DbContextOptions<VentaYaDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<MiembroNegocio> MiembrosNegocio => Set<MiembroNegocio>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ImagenProducto> ImagenesProducto => Set<ImagenProducto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ZonaEnvio> ZonasEnvio => Set<ZonaEnvio>();
    public DbSet<Cupon> Cupones => Set<Cupon>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItemsPedido => Set<ItemPedido>();
    public DbSet<RespuestaRapida> RespuestasRapidas => Set<RespuestaRapida>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.PasswordHash).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();
        });

        mb.Entity<Plan>(e =>
        {
            e.ToTable("planes");
            e.Property(x => x.Nombre).HasMaxLength(30);
            e.Property(x => x.PrecioMensual).HasPrecision(12, 2);
            e.Property(x => x.PrecioAnual).HasPrecision(12, 2);
            e.HasData(
                new Plan { Id = PlanesIds.Gratis, Nombre = "Gratis", PrecioMensual = 0, PrecioAnual = 0, MaxProductos = 10, MaxPedidosMes = 30, MaxRespuestas = 5 },
                new Plan { Id = PlanesIds.Prueba, Nombre = "Prueba", PrecioMensual = 0, PrecioAnual = 0 },
                new Plan { Id = PlanesIds.Pago, Nombre = "Pago", PrecioMensual = 9900, PrecioAnual = 99000 });
        });

        mb.Entity<Negocio>(e =>
        {
            e.ToTable("negocios");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.Telefono).HasMaxLength(30);
            e.Property(x => x.Direccion).HasMaxLength(200);
            e.Property(x => x.LogoUrl).HasMaxLength(300);
            e.Property(x => x.Slug).HasMaxLength(100);
            e.Property(x => x.PlanPeriodo).HasMaxLength(10);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Propietario).WithMany(u => u.NegociosPropios).HasForeignKey(x => x.PropietarioId);
            e.ToTable(t => t.HasCheckConstraint("CK_negocios_plan_periodo", "[plan_periodo] IN ('mensual','anual')"));
        });

        mb.Entity<MiembroNegocio>(e =>
        {
            e.ToTable("miembros_negocio");
            e.Property(x => x.EmailInvitado).HasMaxLength(200);
            e.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TokenInvitacion).HasMaxLength(64);
            e.HasIndex(x => x.TokenInvitacion).IsUnique();
            e.HasIndex(x => new { x.NegocioId, x.UsuarioId }).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_miembros_rol", "[rol] IN ('Empleado')"));
        });

        mb.Entity<Producto>(e =>
        {
            e.ToTable("productos");
            e.Property(x => x.Nombre).HasMaxLength(120);
            e.Property(x => x.Descripcion).HasMaxLength(1000);
            e.Property(x => x.Precio).HasPrecision(12, 2);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_productos_precio", "[precio] >= 0");
                t.HasCheckConstraint("CK_productos_stock", "[stock] IS NULL OR [stock] >= 0");
            });
        });

        mb.Entity<ImagenProducto>(e =>
        {
            e.ToTable("imagenes_producto");
            e.Property(x => x.Url).HasMaxLength(300);
        });

        mb.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Telefono).HasMaxLength(30);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => x.Telefono).IsUnique();
        });

        mb.Entity<ZonaEnvio>(e =>
        {
            e.ToTable("zonas_envio");
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Precio).HasPrecision(12, 2);
            e.ToTable(t => t.HasCheckConstraint("CK_zonas_precio", "[precio] >= 0"));
        });

        mb.Entity<Cupon>(e =>
        {
            e.ToTable("cupones");
            e.Property(x => x.Codigo).HasMaxLength(30);
            e.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Valor).HasPrecision(12, 2);
            e.Property(x => x.MontoMinimo).HasPrecision(12, 2);
            e.HasIndex(x => new { x.NegocioId, x.Codigo }).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_cupones_tipo", "[tipo] IN ('Porcentaje','MontoFijo')");
                t.HasCheckConstraint("CK_cupones_valor", "[valor] > 0");
            });
        });

        mb.Entity<Pedido>(e =>
        {
            e.ToTable("pedidos");
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MetodoPago).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.DireccionEntrega).HasMaxLength(200);
            e.Property(x => x.Notas).HasMaxLength(500);
            e.Property(x => x.CostoEnvio).HasPrecision(12, 2);
            e.Property(x => x.DescuentoAplicado).HasPrecision(12, 2);
            e.HasIndex(x => new { x.NegocioId, x.CreadoEn });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_pedidos_estado", "[estado] IN ('Pendiente','Confirmado','Pagado','Entregado','Cancelado')");
                t.HasCheckConstraint("CK_pedidos_metodo_pago", "[metodo_pago] IN ('Efectivo','Transferencia','MercadoPago')");
            });
        });

        mb.Entity<ItemPedido>(e =>
        {
            e.ToTable("items_pedido");
            e.Property(x => x.PrecioUnitario).HasPrecision(12, 2);
            e.ToTable(t => t.HasCheckConstraint("CK_items_cantidad", "[cantidad] > 0"));
        });

        mb.Entity<RespuestaRapida>(e =>
        {
            e.ToTable("respuestas_rapidas");
            e.Property(x => x.Titulo).HasMaxLength(100);
            e.Property(x => x.Contenido).HasMaxLength(1000);
        });

        // Borrado: por defecto RESTRICT (evita borrados en cadena accidentales y los
        // "múltiples caminos en cascada" que SQL Server no permite). Solo se borran
        // en cascada las partes que no tienen sentido sin su padre.
        foreach (var fk in mb.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            var hijo = fk.DeclaringEntityType.ClrType;
            var padre = fk.PrincipalEntityType.ClrType;
            var cascada =
                (hijo == typeof(ImagenProducto) && padre == typeof(Producto)) ||
                (hijo == typeof(ItemPedido) && padre == typeof(Pedido)) ||
                (hijo == typeof(MiembroNegocio) && padre == typeof(Negocio));
            fk.DeleteBehavior = cascada ? DeleteBehavior.Cascade : DeleteBehavior.Restrict;
        }

        // Columnas en snake_case (negocio_id, creado_en...) como en el DER.
        foreach (var propiedad in mb.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
            propiedad.SetColumnName(ASnakeCase(propiedad.Name));
    }

    private static string ASnakeCase(string nombre) =>
        Regex.Replace(nombre, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}

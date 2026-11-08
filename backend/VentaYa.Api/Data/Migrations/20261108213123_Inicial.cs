using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VentaYa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    precio_mensual = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    precio_anual = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    max_productos = table.Column<int>(type: "int", nullable: true),
                    max_pedidos_mes = table.Column<int>(type: "int", nullable: true),
                    max_respuestas = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "negocios",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    propietario_id = table.Column<int>(type: "int", nullable: false),
                    plan_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    logo_url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    envio_habilitado = table.Column<bool>(type: "bit", nullable: false),
                    plan_periodo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    trial_expira_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_negocios", x => x.id);
                    table.CheckConstraint("CK_negocios_plan_periodo", "[plan_periodo] IN ('mensual','anual')");
                    table.ForeignKey(
                        name: "FK_negocios_planes_plan_id",
                        column: x => x.plan_id,
                        principalTable: "planes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_negocios_usuarios_propietario_id",
                        column: x => x.propietario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cupones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    monto_minimo = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    usos_maximos = table.Column<int>(type: "int", nullable: true),
                    vence_en = table.Column<DateTime>(type: "datetime2", nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cupones", x => x.id);
                    table.CheckConstraint("CK_cupones_tipo", "[tipo] IN ('Porcentaje','MontoFijo')");
                    table.CheckConstraint("CK_cupones_valor", "[valor] > 0");
                    table.ForeignKey(
                        name: "FK_cupones_negocios_negocio_id",
                        column: x => x.negocio_id,
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "miembros_negocio",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: true),
                    email_invitado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    token_invitacion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    aceptado_en = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_miembros_negocio", x => x.id);
                    table.CheckConstraint("CK_miembros_rol", "[rol] IN ('Empleado')");
                    table.ForeignKey(
                        name: "FK_miembros_negocio_negocios_negocio_id",
                        column: x => x.negocio_id,
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_miembros_negocio_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    precio = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    stock = table.Column<int>(type: "int", nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.id);
                    table.CheckConstraint("CK_productos_precio", "[precio] >= 0");
                    table.CheckConstraint("CK_productos_stock", "[stock] IS NULL OR [stock] >= 0");
                    table.ForeignKey(
                        name: "FK_productos_negocios_negocio_id",
                        column: x => x.negocio_id,
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "respuestas_rapidas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    contenido = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_respuestas_rapidas", x => x.id);
                    table.ForeignKey(
                        name: "FK_respuestas_rapidas_negocios_negocio_id",
                        column: x => x.negocio_id,
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "zonas_envio",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    precio = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zonas_envio", x => x.id);
                    table.CheckConstraint("CK_zonas_precio", "[precio] >= 0");
                    table.ForeignKey(
                        name: "FK_zonas_envio_negocios_negocio_id",
                        column: x => x.negocio_id,
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "imagenes_producto",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    producto_id = table.Column<int>(type: "int", nullable: false),
                    url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imagenes_producto", x => x.id);
                    table.ForeignKey(
                        name: "FK_imagenes_producto_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pedidos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    cliente_id = table.Column<int>(type: "int", nullable: false),
                    zona_envio_id = table.Column<int>(type: "int", nullable: true),
                    cupon_id = table.Column<int>(type: "int", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    metodo_pago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    direccion_entrega = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    costo_envio = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    descuento_aplicado = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos", x => x.id);
                    table.CheckConstraint("CK_pedidos_estado", "[estado] IN ('Pendiente','Confirmado','Pagado','Entregado','Cancelado')");
                    table.CheckConstraint("CK_pedidos_metodo_pago", "[metodo_pago] IN ('Efectivo','Transferencia','MercadoPago')");
                    table.ForeignKey(
                        name: "FK_pedidos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_cupones_cupon_id",
                        column: x => x.cupon_id,
                        principalTable: "cupones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_negocios_negocio_id",
                        column: x => x.negocio_id,
                        principalTable: "negocios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_zonas_envio_zona_envio_id",
                        column: x => x.zona_envio_id,
                        principalTable: "zonas_envio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "items_pedido",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pedido_id = table.Column<int>(type: "int", nullable: false),
                    producto_id = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items_pedido", x => x.id);
                    table.CheckConstraint("CK_items_cantidad", "[cantidad] > 0");
                    table.ForeignKey(
                        name: "FK_items_pedido_pedidos_pedido_id",
                        column: x => x.pedido_id,
                        principalTable: "pedidos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_pedido_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "planes",
                columns: new[] { "id", "max_pedidos_mes", "max_productos", "max_respuestas", "nombre", "precio_anual", "precio_mensual" },
                values: new object[,]
                {
                    { 1, 30, 10, 5, "Gratis", 0m, 0m },
                    { 2, null, null, null, "Prueba", 0m, 0m },
                    { 3, null, null, null, "Pago", 99000m, 9900m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_clientes_telefono",
                table: "clientes",
                column: "telefono",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cupones_negocio_id_codigo",
                table: "cupones",
                columns: new[] { "negocio_id", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_imagenes_producto_producto_id",
                table: "imagenes_producto",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_pedido_pedido_id",
                table: "items_pedido",
                column: "pedido_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_pedido_producto_id",
                table: "items_pedido",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_miembros_negocio_negocio_id_usuario_id",
                table: "miembros_negocio",
                columns: new[] { "negocio_id", "usuario_id" },
                unique: true,
                filter: "[usuario_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_miembros_negocio_token_invitacion",
                table: "miembros_negocio",
                column: "token_invitacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_miembros_negocio_usuario_id",
                table: "miembros_negocio",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_negocios_plan_id",
                table: "negocios",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_negocios_propietario_id",
                table: "negocios",
                column: "propietario_id");

            migrationBuilder.CreateIndex(
                name: "IX_negocios_slug",
                table: "negocios",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_cliente_id",
                table: "pedidos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_cupon_id",
                table: "pedidos",
                column: "cupon_id");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_negocio_id_creado_en",
                table: "pedidos",
                columns: new[] { "negocio_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_zona_envio_id",
                table: "pedidos",
                column: "zona_envio_id");

            migrationBuilder.CreateIndex(
                name: "IX_productos_negocio_id",
                table: "productos",
                column: "negocio_id");

            migrationBuilder.CreateIndex(
                name: "IX_respuestas_rapidas_negocio_id",
                table: "respuestas_rapidas",
                column: "negocio_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_zonas_envio_negocio_id",
                table: "zonas_envio",
                column: "negocio_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imagenes_producto");

            migrationBuilder.DropTable(
                name: "items_pedido");

            migrationBuilder.DropTable(
                name: "miembros_negocio");

            migrationBuilder.DropTable(
                name: "respuestas_rapidas");

            migrationBuilder.DropTable(
                name: "pedidos");

            migrationBuilder.DropTable(
                name: "productos");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "cupones");

            migrationBuilder.DropTable(
                name: "zonas_envio");

            migrationBuilder.DropTable(
                name: "negocios");

            migrationBuilder.DropTable(
                name: "planes");

            migrationBuilder.DropTable(
                name: "usuarios");
        }
    }
}

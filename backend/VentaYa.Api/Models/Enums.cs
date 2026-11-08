namespace VentaYa.Api.Models;

// Dominios cerrados: se guardan como texto en la BD con una restricción CHECK
// (ver VentaYaDbContext) en vez de tablas de 3 filas.

public enum EstadoPedido { Pendiente, Confirmado, Pagado, Entregado, Cancelado }

public enum MetodoPago { Efectivo, Transferencia, MercadoPago }

public enum TipoCupon { Porcentaje, MontoFijo }

public enum RolMiembro { Empleado }

/// <summary>Ids fijos de los planes sembrados en la migración inicial.</summary>
public static class PlanesIds
{
    public const int Gratis = 1;
    public const int Prueba = 2;
    public const int Pago = 3;
}

namespace VentaYa.Api.Models;

public class Producto
{
    public int Id { get; set; }
    public int NegocioId { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    /// <summary>NULL = el negocio no controla stock de este producto.</summary>
    public int? Stock { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public Negocio Negocio { get; set; } = null!;
    public List<ImagenProducto> Imagenes { get; set; } = [];
}

public class ImagenProducto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Url { get; set; } = "";
    public int Orden { get; set; }

    public Producto Producto { get; set; } = null!;
}

public class RespuestaRapida
{
    public int Id { get; set; }
    public int NegocioId { get; set; }
    public string Titulo { get; set; } = "";
    /// <summary>Texto con variables {nombre}, {total} y {link}.</summary>
    public string Contenido { get; set; } = "";

    public Negocio Negocio { get; set; } = null!;
}

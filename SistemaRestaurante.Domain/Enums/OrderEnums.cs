namespace SistemaRestaurante.Domain.Enums
{
    /// <summary>Cuenta de una mesa.</summary>
    public enum OrderStatus
    {
        /// <summary>Clientes sentados; se pueden pedir platos.</summary>
        Open = 0,
        /// <summary>Cuenta cobrada; la mesa queda libre.</summary>
        Closed = 1,
        /// <summary>Anulada por el encargado (con motivo).</summary>
        Cancelled = 2
    }

    /// <summary>
    /// Recorrido de cada plato pedido:
    /// Draft → (Held →) Sent → Preparing → Ready → Served, o Cancelled.
    /// </summary>
    public enum OrderItemStatus
    {
        /// <summary>Apuntado por el camarero, todavía no enviado. Cocina no lo ve.</summary>
        Draft = 0,
        /// <summary>Enviado pero retenido hasta que el camarero lo "marche" (p. ej. los segundos).</summary>
        Held = 1,
        /// <summary>En la pantalla de cocina/barra, pendiente de empezar.</summary>
        Sent = 2,
        Preparing = 3,
        /// <summary>Listo en el pase para servir.</summary>
        Ready = 4,
        Served = 5,
        Cancelled = 6
    }

    /// <summary>Tiempo del servicio en el que sale el plato.</summary>
    public enum OrderCourse
    {
        /// <summary>Sale en cuanto se pide (bebidas, panes...).</summary>
        Immediate = 0,
        Starter = 1,
        Main = 2,
        Dessert = 3
    }

    public enum PaymentMethod
    {
        Cash = 0,
        Card = 1,
        Transfer = 2,
        Other = 3
    }

    /// <summary>Tipo de zona de preparación; sirve para elegir la zona por defecto de cada plato.</summary>
    public enum StationType
    {
        Kitchen = 0,
        Bar = 1,
        Other = 2
    }

    /// <summary>Los 14 alérgenos de declaración obligatoria en la UE (referencia habitual también en Colombia).</summary>
    [Flags]
    public enum Allergens
    {
        None = 0,
        Gluten = 1 << 0,
        Crustaceans = 1 << 1,
        Eggs = 1 << 2,
        Fish = 1 << 3,
        Peanuts = 1 << 4,
        Soy = 1 << 5,
        Milk = 1 << 6,
        TreeNuts = 1 << 7,
        Celery = 1 << 8,
        Mustard = 1 << 9,
        Sesame = 1 << 10,
        Sulphites = 1 << 11,
        Lupin = 1 << 12,
        Molluscs = 1 << 13
    }
}

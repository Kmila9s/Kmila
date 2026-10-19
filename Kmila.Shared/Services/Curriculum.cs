namespace Kmila.Shared.Services;

/// <summary>
/// Static metadata for the ESCOM ISC-2020 asignaturas the Learn catalog groups
/// by in "By asignatura" mode. Keeps the fixed display order, the i18n label
/// keys, and the color-variant mapping in one place so <c>Learn.razor</c> and
/// <c>LessonView.razor</c> stay consistent. The asignatura id itself lives on
/// <c>LessonModule.Asignatura</c> in the manifest.
/// </summary>
public static class Curriculum
{
    /// <summary>
    /// Asignatura ids in curriculum order (roughly the order a student meets
    /// them in the ISC-2020 plan). Modules with an id not in this list, or with
    /// no asignatura, sort after these under a catch-all track.
    /// </summary>
    public static readonly string[] AsignaturaOrder =
    {
        "matematicas-discretas",
        "fundamentos-diseno-digital",
        "diseno-sistemas-digitales",
        "arquitectura-computadoras",
        "sistemas-chip"
    };

    /// <summary>Sort index for an asignatura id; unknown/empty ids sort last.</summary>
    public static int OrderOf(string? asignatura)
    {
        if (string.IsNullOrEmpty(asignatura)) return int.MaxValue;
        var idx = Array.IndexOf(AsignaturaOrder, asignatura);
        return idx < 0 ? int.MaxValue - 1 : idx;
    }

    /// <summary>Traductor key for an asignatura's display name.</summary>
    public static string LabelKey(string? asignatura) => asignatura switch
    {
        "matematicas-discretas"      => "LEARN_ASIGNATURA_MATH",
        "fundamentos-diseno-digital" => "LEARN_ASIGNATURA_FUND",
        "diseno-sistemas-digitales"  => "LEARN_ASIGNATURA_DSD",
        "arquitectura-computadoras"  => "LEARN_ASIGNATURA_ARQ",
        "sistemas-chip"              => "LEARN_ASIGNATURA_SOC",
        _                            => "LEARN_ASIGNATURA_OTHER"
    };

    /// <summary>CSS color-variant suffix (matches .learn-card__icon--* variants).</summary>
    public static string ColorVariant(string? asignatura) => asignatura switch
    {
        "matematicas-discretas"      => "success",
        "fundamentos-diseno-digital" => "primary",
        "diseno-sistemas-digitales"  => "info",
        "arquitectura-computadoras"  => "warning",
        "sistemas-chip"              => "danger",
        _                            => "primary"
    };
}

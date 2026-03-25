namespace Entrevista.Helpers
{
    /// <summary>
    /// Utilitario estático que centraliza la lógica de cálculo del
    /// nivel técnico de un candidato basado en su puntaje promedio.
    ///
    /// UMBRALES:
    ///   Senior → promedio >= 8.0
    ///   Mid    → promedio >= 5.0
    ///   Junior → promedio  < 5.0
    ///
    /// Usar este helper en TODOS los controladores y ViewModels para
    /// garantizar consistencia en toda la aplicación.
    /// </summary>
    public static class NivelCalculator
    {
        // ────────────────────────────────────────────────────────────
        // Constantes de umbral
        // ────────────────────────────────────────────────────────────

        private const double UMBRAL_SENIOR = 8.0;
        private const double UMBRAL_MID = 5.0;

        // ────────────────────────────────────────────────────────────
        // Constantes de nombre de nivel
        // ────────────────────────────────────────────────────────────

        public const string NIVEL_SENIOR = "Senior";
        public const string NIVEL_MID = "Mid";
        public const string NIVEL_JUNIOR = "Junior";

        // ====================================================================
        // MÉTODOS PÚBLICOS
        // ====================================================================

        /// <summary>
        /// Calcula el nivel técnico de un candidato a partir de su promedio.
        /// </summary>
        /// <param name="promedio">Puntaje promedio de 0.0 a 10.0.</param>
        /// <returns>"Senior", "Mid" o "Junior".</returns>
        public static string Calcular(double promedio)
        {
            if (promedio >= UMBRAL_SENIOR) return NIVEL_SENIOR;
            if (promedio >= UMBRAL_MID) return NIVEL_MID;
            return NIVEL_JUNIOR;
        }

        /// <summary>
        /// Retorna la clase CSS del badge de nivel para la vista.
        /// </summary>
        /// <param name="nivel">Nombre del nivel ("Senior", "Mid", "Junior").</param>
        public static string BadgeClass(string nivel)
        {
            switch (nivel)
            {
                case NIVEL_SENIOR: return "sde-badge-green";
                case NIVEL_MID: return "sde-badge-yellow";
                default: return "sde-badge-red";
            }
        }

        /// <summary>
        /// Retorna el icono Bootstrap asociado al nivel.
        /// </summary>
        /// <param name="nivel">Nombre del nivel.</param>
        public static string Icono(string nivel)
        {
            switch (nivel)
            {
                case NIVEL_SENIOR: return "bi-award-fill";
                case NIVEL_MID: return "bi-lightning-fill";
                default: return "bi-person-fill";
            }
        }

        /// <summary>
        /// Retorna el color CSS del puntaje según el valor.
        /// </summary>
        /// <param name="promedio">Puntaje de 0.0 a 10.0.</param>
        public static string ColorPuntaje(double promedio)
        {
            if (promedio >= UMBRAL_SENIOR) return "var(--sde-success)";
            if (promedio >= UMBRAL_MID) return "var(--sde-warning)";
            return "var(--sde-danger)";
        }
    }
}
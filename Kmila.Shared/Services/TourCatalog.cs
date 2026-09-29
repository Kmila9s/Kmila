// TourCatalog — central registry of welcome-tour step sets, one per route.
//
// MainLayout calls GetForRoute(currentPath) on every navigation. If the
// returned RouteTour's StorageKey hasn't been seen yet (per
// localStorage.kmila.tutorialSeen.<id>), the engine fires the steps.
//
// Step definitions intentionally use stable CSS-class selectors that
// already exist in the codebase (e.g. `.run-bar__fpga`, `.kmila-dock__tabs`,
// `.projects-search`). The Topbar uses data-tour="…" markers because its
// child components are wrapped in spans for that purpose. If a selector
// breaks, the engine simply skips that step — the tour still finishes.

using System;
using System.Collections.Generic;

namespace Kmila.Shared.Services;

/// <summary>One step of a welcome tour.</summary>
/// <param name="Target">CSS selector or data-tour marker the step points at, or null for a centered step.</param>
/// <param name="Title">Step title.</param>
/// <param name="Body">Step body text.</param>
public sealed record TourStep(string? Target, string Title, string Body);

/// <summary>A tour authored for a specific route, keyed by its storage id.</summary>
/// <param name="StorageKey">Per-route id used to track whether the tour has been seen.</param>
/// <param name="Steps">Ordered steps of the tour.</param>
public sealed record RouteTour(string StorageKey, IReadOnlyList<TourStep> Steps);

/// <summary>Central registry of welcome-tour step sets, one per route.</summary>
public static class TourCatalog
{
    private const string Prefix = "kmila.tutorialSeen";

    /// <summary>Storage key for the one-time first-run welcome (fired on the
    /// first app open on any non-Home route). Public so MainLayout can gate it.</summary>
    public const string WelcomeStorageKey = Prefix + ".welcome";

    /// <summary>
    /// Resolves the path to a per-route tour, or null if the route has no
    /// tour authored. Match order matters: longest/most-specific paths
    /// first. Every top-level view and every detail/leaf route has a tour so
    /// no view is left un-introduced.
    /// </summary>
    public static RouteTour? GetForRoute(string path, Func<string, string> t)
    {
        if (string.IsNullOrWhiteSpace(path)) path = "/";
        path = path.TrimEnd('/');
        if (path.Length == 0) path = "/";

        var lower = path.ToLowerInvariant();

        // Prefix (parameterised) routes — most specific first.
        if (lower.StartsWith("/editor/", StringComparison.Ordinal))            return Editor(t);
        if (lower.StartsWith("/visual/", StringComparison.Ordinal))            return Visual(t);
        if (lower.StartsWith("/concepts/exercise/", StringComparison.Ordinal)) return ConceptExercise(t);
        if (lower.StartsWith("/concepts/", StringComparison.Ordinal))          return ConceptLesson(t);
        if (lower.StartsWith("/learn/", StringComparison.Ordinal))             return Lesson(t);
        if (lower.StartsWith("/practice/", StringComparison.Ordinal))          return PracticeExercise(t);
        if (lower.StartsWith("/projects/", StringComparison.Ordinal))          return Projects(t);

        // Exact top-level routes.
        return lower switch
        {
            "/"          => Home(t),
            "/projects"  => Projects(t),
            "/learn"     => Learn(t),
            "/concepts"  => Concepts(t),
            "/practice"  => Practice(t),
            "/settings"  => Settings(t),
            "/logs"      => Logs(t),
            "/privacy"   => Privacy(t),
            _            => null,
        };
    }

    // ── First-run welcome ──────────────────────────────────────────────
    /// <summary>A single centered welcome card, fired once on the very first
    /// app open regardless of which route the user landed on.</summary>
    public static RouteTour Welcome(Func<string,string> t) => new(
        WelcomeStorageKey,
        new[]
        {
            new TourStep(null, t("TUTORIAL_WELCOME_TITLE"), t("TUTORIAL_WELCOME_BODY")),
        });

    // ── Home / first-boot welcome ─────────────────────────────────────
    private static RouteTour Home(Func<string,string> t) => new(
        $"{Prefix}.home",
        new[]
        {
            new TourStep(null,                        t("TUTORIAL_WELCOME_TITLE"),  t("TUTORIAL_WELCOME_BODY")),
            new TourStep("a[href='/projects']",       t("TUTORIAL_PROJECTS_TITLE"), t("TUTORIAL_PROJECTS_BODY")),
            new TourStep("a[href='/learn']",          t("TUTORIAL_LEARN_TITLE"),    t("TUTORIAL_LEARN_BODY")),
            new TourStep("a[href='/concepts']",       t("TUTORIAL_CONCEPTS_TITLE"), t("TUTORIAL_CONCEPTS_BODY")),
            new TourStep("a[href='/practice']",       t("TUTORIAL_PRACTICE_TITLE"), t("TUTORIAL_PRACTICE_BODY")),
            new TourStep("a[href='/settings']",       t("TUTORIAL_SETTINGS_TITLE"), t("TUTORIAL_SETTINGS_BODY")),
            // The configurations pill only renders on project routes, so it isn't
            // present on Home — the profile menu is the right anchor here. (The
            // pill is explained by the Editor tour where it actually appears.)
            new TourStep("[data-tour='topbar-profile']",        t("TUTORIAL_TOPBAR_PROFILE_TITLE"), t("TUTORIAL_TOPBAR_PROFILE_BODY")),
            new TourStep(".kmila-shell__footer a",    t("TUTORIAL_FOOTER_TITLE"),   t("TUTORIAL_FOOTER_BODY")),
            new TourStep(null,                        t("TUTORIAL_FINAL_TITLE"),    t("TUTORIAL_FINAL_BODY")),
        });

    // ── Projects view ─────────────────────────────────────────────────
    private static RouteTour Projects(Func<string,string> t) => new(
        $"{Prefix}.projects",
        new[]
        {
            new TourStep(null,                  t("TUTORIAL_PROJECTSVIEW_INTRO_TITLE"),  t("TUTORIAL_PROJECTSVIEW_INTRO_BODY")),
            new TourStep(".projects-search",    t("TUTORIAL_PROJECTSVIEW_SEARCH_TITLE"), t("TUTORIAL_PROJECTSVIEW_SEARCH_BODY")),
            new TourStep(".projects-cta",       t("TUTORIAL_PROJECTSVIEW_CREATE_TITLE"), t("TUTORIAL_PROJECTSVIEW_CREATE_BODY")),
            new TourStep(".projects-sample",    t("TUTORIAL_PROJECTSVIEW_SAMPLE_TITLE"), t("TUTORIAL_PROJECTSVIEW_SAMPLE_BODY")),
            new TourStep(".project-card",       t("TUTORIAL_PROJECTSVIEW_CARD_TITLE"),   t("TUTORIAL_PROJECTSVIEW_CARD_BODY")),
        });

    // ── Editor view ───────────────────────────────────────────────────
    private static RouteTour Editor(Func<string,string> t) => new(
        $"{Prefix}.editor",
        new[]
        {
            new TourStep(null,                                 t("TUTORIAL_EDITOR_INTRO_TITLE"),   t("TUTORIAL_EDITOR_INTRO_BODY")),
            new TourStep(".editor-workbench__view-tabs",       t("TUTORIAL_EDITOR_VIEWS_TITLE"),   t("TUTORIAL_EDITOR_VIEWS_BODY")),
            new TourStep(".editor-workbench__body",            t("TUTORIAL_EDITOR_CODE_TITLE"),    t("TUTORIAL_EDITOR_CODE_BODY")),
            new TourStep(".run-bar",                           t("TUTORIAL_EDITOR_RUNBAR_TITLE"),  t("TUTORIAL_EDITOR_RUNBAR_BODY")),
            new TourStep(".run-bar__fpga",                     t("TUTORIAL_EDITOR_FPGA_TITLE"),    t("TUTORIAL_EDITOR_FPGA_BODY")),
            new TourStep(".kmila-dock__tabs",                  t("TUTORIAL_EDITOR_DOCK_TITLE"),    t("TUTORIAL_EDITOR_DOCK_BODY")),
            new TourStep(".kmila-dock__grabber",               t("TUTORIAL_EDITOR_GRABBER_TITLE"), t("TUTORIAL_EDITOR_GRABBER_BODY")),
            new TourStep(null,                                 t("TUTORIAL_EDITOR_FINAL_TITLE"),   t("TUTORIAL_EDITOR_FINAL_BODY")),
        });

    // ── Learn view ────────────────────────────────────────────────────
    private static RouteTour Learn(Func<string,string> t) => new(
        $"{Prefix}.learn",
        new[]
        {
            new TourStep(null,                       t("TUTORIAL_LEARNVIEW_INTRO_TITLE"),    t("TUTORIAL_LEARNVIEW_INTRO_BODY")),
            new TourStep(".learn-hero__progress",    t("TUTORIAL_LEARNVIEW_PROGRESS_TITLE"), t("TUTORIAL_LEARNVIEW_PROGRESS_BODY")),
            new TourStep(".learn-hero__search",      t("TUTORIAL_LEARNVIEW_SEARCH_TITLE"),   t("TUTORIAL_LEARNVIEW_SEARCH_BODY")),
            new TourStep(".learn-track",             t("TUTORIAL_LEARNVIEW_TRACK_TITLE"),    t("TUTORIAL_LEARNVIEW_TRACK_BODY")),
        });

    // ── Concepts view ─────────────────────────────────────────────────
    private static RouteTour Concepts(Func<string,string> t) => new(
        $"{Prefix}.concepts",
        new[]
        {
            new TourStep(null,                  t("TUTORIAL_CONCEPTSVIEW_INTRO_TITLE"),   t("TUTORIAL_CONCEPTSVIEW_INTRO_BODY")),
            new TourStep(".learn-track",        t("TUTORIAL_CONCEPTSVIEW_TRACK_TITLE"),   t("TUTORIAL_CONCEPTSVIEW_TRACK_BODY")),
            new TourStep(".learn-hero__search", t("TUTORIAL_CONCEPTSVIEW_SEARCH_TITLE"),  t("TUTORIAL_CONCEPTSVIEW_SEARCH_BODY")),
        });

    // ── Practice view ─────────────────────────────────────────────────
    private static RouteTour Practice(Func<string,string> t) => new(
        $"{Prefix}.practice",
        new[]
        {
            new TourStep(null,                       t("TUTORIAL_PRACTICEVIEW_INTRO_TITLE"),   t("TUTORIAL_PRACTICEVIEW_INTRO_BODY")),
            new TourStep(".catalog-filters",         t("TUTORIAL_PRACTICEVIEW_FILTERS_TITLE"), t("TUTORIAL_PRACTICEVIEW_FILTERS_BODY")),
            new TourStep(".learn-track",             t("TUTORIAL_PRACTICEVIEW_TRACK_TITLE"),   t("TUTORIAL_PRACTICEVIEW_TRACK_BODY")),
        });

    // ── Settings view ─────────────────────────────────────────────────
    private static RouteTour Settings(Func<string,string> t) => new(
        $"{Prefix}.settings",
        new[]
        {
            new TourStep(null,             t("TUTORIAL_SETTINGSVIEW_INTRO_TITLE"),      t("TUTORIAL_SETTINGSVIEW_INTRO_BODY")),
            new TourStep(".settings-card", t("TUTORIAL_SETTINGSVIEW_APPEARANCE_TITLE"), t("TUTORIAL_SETTINGSVIEW_APPEARANCE_BODY")),
            new TourStep("[data-tour='settings-simulation']", t("TUTORIAL_SETTINGSVIEW_SIMULATION_TITLE"), t("TUTORIAL_SETTINGSVIEW_SIMULATION_BODY")),
            new TourStep("a[href='/privacy']", t("TUTORIAL_SETTINGSVIEW_PRIVACY_TITLE"), t("TUTORIAL_SETTINGSVIEW_PRIVACY_BODY")),
        });

    // ── Visual simulator (/visual/{id}) ───────────────────────────────
    private static RouteTour Visual(Func<string,string> t) => new(
        $"{Prefix}.visual",
        new[]
        {
            new TourStep(null,                              t("TUTORIAL_VISUAL_INTRO_TITLE"),   t("TUTORIAL_VISUAL_INTRO_BODY")),
            new TourStep(".visual-page__toolbar",           t("TUTORIAL_VISUAL_TOOLBAR_TITLE"), t("TUTORIAL_VISUAL_TOOLBAR_BODY")),
            new TourStep(".visual-page__palette",           t("TUTORIAL_VISUAL_PALETTE_TITLE"), t("TUTORIAL_VISUAL_PALETTE_BODY")),
            new TourStep(".visual-page__canvas-wrap",       t("TUTORIAL_VISUAL_CANVAS_TITLE"),  t("TUTORIAL_VISUAL_CANVAS_BODY")),
            new TourStep("[data-testid='visual-wire']",     t("TUTORIAL_VISUAL_WIRE_TITLE"),    t("TUTORIAL_VISUAL_WIRE_BODY")),
            new TourStep("[data-testid='visual-run']",      t("TUTORIAL_VISUAL_RUN_TITLE"),     t("TUTORIAL_VISUAL_RUN_BODY")),
            new TourStep("[data-testid='visual-more-toggle']", t("TUTORIAL_VISUAL_EXPORT_TITLE"), t("TUTORIAL_VISUAL_EXPORT_BODY")),
            new TourStep(null,                              t("TUTORIAL_VISUAL_FINAL_TITLE"),   t("TUTORIAL_VISUAL_FINAL_BODY")),
        });

    // ── Lesson detail (/learn/{module}/{lesson}) ──────────────────────
    private static RouteTour Lesson(Func<string,string> t) => new(
        $"{Prefix}.lesson",
        new[]
        {
            new TourStep(null,                              t("TUTORIAL_LESSON_INTRO_TITLE"),    t("TUTORIAL_LESSON_INTRO_BODY")),
            new TourStep(".lesson-body",                    t("TUTORIAL_LESSON_BODY_TITLE"),     t("TUTORIAL_LESSON_BODY_BODY")),
            new TourStep("[data-tour='lesson-complete-btn']", t("TUTORIAL_LESSON_COMPLETE_TITLE"), t("TUTORIAL_LESSON_COMPLETE_BODY")),
            new TourStep(".lesson-footer__nav",             t("TUTORIAL_LESSON_NAV_TITLE"),      t("TUTORIAL_LESSON_NAV_BODY")),
            new TourStep(null,                              t("TUTORIAL_LESSON_FINAL_TITLE"),    t("TUTORIAL_LESSON_FINAL_BODY")),
        });

    // ── Concept lesson (/concepts/{module}/{lesson}) ──────────────────
    private static RouteTour ConceptLesson(Func<string,string> t) => new(
        $"{Prefix}.conceptlesson",
        new[]
        {
            new TourStep(null,                               t("TUTORIAL_CONCEPTLESSON_INTRO_TITLE"),    t("TUTORIAL_CONCEPTLESSON_INTRO_BODY")),
            new TourStep(".lesson-body",                     t("TUTORIAL_CONCEPTLESSON_BODY_TITLE"),     t("TUTORIAL_CONCEPTLESSON_BODY_BODY")),
            new TourStep("[data-tour='concept-complete-btn']", t("TUTORIAL_CONCEPTLESSON_COMPLETE_TITLE"), t("TUTORIAL_CONCEPTLESSON_COMPLETE_BODY")),
            new TourStep(".lesson-footer__nav",              t("TUTORIAL_CONCEPTLESSON_NAV_TITLE"),      t("TUTORIAL_CONCEPTLESSON_NAV_BODY")),
        });

    // ── Concept exercise (/concepts/exercise/{id}) ────────────────────
    private static RouteTour ConceptExercise(Func<string,string> t) => new(
        $"{Prefix}.conceptexercise",
        new[]
        {
            new TourStep(null,                                 t("TUTORIAL_CONCEPTEXERCISE_INTRO_TITLE"),     t("TUTORIAL_CONCEPTEXERCISE_INTRO_BODY")),
            new TourStep(".kmila-exercise-board",              t("TUTORIAL_CONCEPTEXERCISE_WORKSPACE_TITLE"), t("TUTORIAL_CONCEPTEXERCISE_WORKSPACE_BODY")),
            new TourStep("[data-tour='exercise-run']",         t("TUTORIAL_CONCEPTEXERCISE_RUN_TITLE"),       t("TUTORIAL_CONCEPTEXERCISE_RUN_BODY")),
            new TourStep("[data-tour='exercise-complete-btn']",t("TUTORIAL_CONCEPTEXERCISE_COMPLETE_TITLE"),  t("TUTORIAL_CONCEPTEXERCISE_COMPLETE_BODY")),
            new TourStep(null,                                 t("TUTORIAL_CONCEPTEXERCISE_FINAL_TITLE"),     t("TUTORIAL_CONCEPTEXERCISE_FINAL_BODY")),
        });

    // ── Practice exercise (/practice/{id}) ────────────────────────────
    private static RouteTour PracticeExercise(Func<string,string> t) => new(
        $"{Prefix}.practiceexercise",
        new[]
        {
            new TourStep(null,                         t("TUTORIAL_PRACTICEEX_INTRO_TITLE"),  t("TUTORIAL_PRACTICEEX_INTRO_BODY")),
            new TourStep(".practice-pane--brief",      t("TUTORIAL_PRACTICEEX_BRIEF_TITLE"),  t("TUTORIAL_PRACTICEEX_BRIEF_BODY")),
            new TourStep(".practice-editor-frame",     t("TUTORIAL_PRACTICEEX_EDITOR_TITLE"), t("TUTORIAL_PRACTICEEX_EDITOR_BODY")),
            new TourStep(".practice-actions__submit",  t("TUTORIAL_PRACTICEEX_SUBMIT_TITLE"), t("TUTORIAL_PRACTICEEX_SUBMIT_BODY")),
            new TourStep(null,                         t("TUTORIAL_PRACTICEEX_FINAL_TITLE"),  t("TUTORIAL_PRACTICEEX_FINAL_BODY")),
        });

    // ── Logs (/logs) ──────────────────────────────────────────────────
    private static RouteTour Logs(Func<string,string> t) => new(
        $"{Prefix}.logs",
        new[]
        {
            new TourStep(null,                       t("TUTORIAL_LOGS_INTRO_TITLE"),  t("TUTORIAL_LOGS_INTRO_BODY")),
            new TourStep(".logs-page__search",       t("TUTORIAL_LOGS_SEARCH_TITLE"), t("TUTORIAL_LOGS_SEARCH_BODY")),
            new TourStep(".logs-page__level",        t("TUTORIAL_LOGS_LEVEL_TITLE"),  t("TUTORIAL_LOGS_LEVEL_BODY")),
            new TourStep("[data-tour='logs-export']", t("TUTORIAL_LOGS_EXPORT_TITLE"), t("TUTORIAL_LOGS_EXPORT_BODY")),
            new TourStep("[data-tour='logs-clear']",  t("TUTORIAL_LOGS_CLEAR_TITLE"),  t("TUTORIAL_LOGS_CLEAR_BODY")),
        });

    // ── Privacy (/privacy) ────────────────────────────────────────────
    private static RouteTour Privacy(Func<string,string> t) => new(
        $"{Prefix}.privacy",
        new[]
        {
            new TourStep(null,                          t("TUTORIAL_PRIVACY_INTRO_TITLE"),    t("TUTORIAL_PRIVACY_INTRO_BODY")),
            new TourStep("[data-tour='privacy-local']", t("TUTORIAL_PRIVACY_LOCAL_TITLE"),    t("TUTORIAL_PRIVACY_LOCAL_BODY")),
            new TourStep("[data-tour='privacy-feedback']", t("TUTORIAL_PRIVACY_FEEDBACK_TITLE"), t("TUTORIAL_PRIVACY_FEEDBACK_BODY")),
            new TourStep("[data-tour='privacy-cdn']",   t("TUTORIAL_PRIVACY_CDN_TITLE"),      t("TUTORIAL_PRIVACY_CDN_BODY")),
            new TourStep("[data-tour='privacy-rights']", t("TUTORIAL_PRIVACY_RIGHTS_TITLE"),  t("TUTORIAL_PRIVACY_RIGHTS_BODY")),
            new TourStep(null,                          t("TUTORIAL_PRIVACY_FINAL_TITLE"),    t("TUTORIAL_PRIVACY_FINAL_BODY")),
        });
}

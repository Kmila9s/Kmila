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

public sealed record TourStep(string? Target, string Title, string Body);

public sealed record RouteTour(string StorageKey, IReadOnlyList<TourStep> Steps);

public static class TourCatalog
{
    private const string Prefix = "kmila.tutorialSeen";

    /// <summary>
    /// Resolves the path to a per-route tour, or null if the route has no
    /// tour authored. Match order matters: longest/most-specific paths
    /// first.
    /// </summary>
    public static RouteTour? GetForRoute(string path, Func<string, string> t)
    {
        if (string.IsNullOrWhiteSpace(path)) path = "/";
        path = path.TrimEnd('/');
        if (path == "") path = "/";

        if (path.StartsWith("/editor/", StringComparison.OrdinalIgnoreCase))
            return Editor(t);

        return path.ToLowerInvariant() switch
        {
            "/"          => Home(t),
            "/projects"  => Projects(t),
            "/learn"     => Learn(t),
            "/concepts"  => Concepts(t),
            "/practice"  => Practice(t),
            "/settings"  => Settings(t),
            _            => null,
        };
    }

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
            new TourStep("[data-tour='topbar-configurations']", t("TUTORIAL_TOPBAR_CONFIG_TITLE"), t("TUTORIAL_TOPBAR_CONFIG_BODY")),
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
            new TourStep(".settings-card:nth-of-type(3)", t("TUTORIAL_SETTINGSVIEW_SIMULATION_TITLE"), t("TUTORIAL_SETTINGSVIEW_SIMULATION_BODY")),
            new TourStep("a[href='/privacy']", t("TUTORIAL_SETTINGSVIEW_PRIVACY_TITLE"), t("TUTORIAL_SETTINGSVIEW_PRIVACY_BODY")),
        });
}

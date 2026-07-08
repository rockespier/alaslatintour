## En el appsettings.json

{
  "WordPressConfig": {
    "BaseUrl": "https://alasglobaltour.rtres.net/wp-json/wp/v2/posts?_embed=1",
    "Username": "dotnet-bff-service",
    "AppPassword": "^xPE4#77tv6hHR)I^yh7(X%T"
  }
}


## En el program.cs

var wpConfig = builder.Configuration.GetSection("WordPressConfig");
var encodedAuth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{wpConfig["Username"]}:{wpConfig["AppPassword"]}"));

builder.Services.AddHttpClient<IWordPressService, WordPressService>(client =>
{
    client.BaseAddress = new Uri(wpConfig["BaseUrl"]);
    // Agregamos la cabecera de Authorization nativa
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedAuth);
    // User Agent profesional
    client.DefaultRequestHeaders.Add("User-Agent", "AlasBFF-DotNet9");
});


<?php
// Hook para inicializar los campos en la API REST
add_action('init', 'rtres_register_post_meta_fields');

function rtres_register_post_meta_fields() {
    
    // 1. Campo: Cargo del Autor (Author Role)
    register_post_meta('post', 'author_role', [
        'show_in_rest' => true,
        'single'       => true,
        'type'         => 'string',
        'default'      => 'Redactor ALAS',
        'sanitize_callback' => 'sanitize_text_field'
    ]);

    // 2. Campo: Tiempo de Lectura (Read Time en minutos)
    register_post_meta('post', 'read_time_minutes', [
        'show_in_rest' => true,
        'single'       => true,
        'type'         => 'integer',
        'default'      => 3,
        'sanitize_callback' => 'absint'
    ]);
	
	// Agrégalo dentro de tu función existente donde registramos 'author_role'
	register_post_meta('post', 'show_ranking', [
		'show_in_rest' => true,
		'single'       => true,
		'type'         => 'boolean',
		'default'      => false
	]);
}

<?php
// EL MÉTODO BULLETPROOF: Inyectar campos directamente al JSON final
add_action('rest_api_init', 'alas_inject_custom_fields_to_api');

function alas_inject_custom_fields_to_api() {
    
    // 1. Inyectamos el Cargo del Autor
    register_rest_field('post', 'author_role', [
        'get_callback' => function($post_arr) {
            $val = get_post_meta($post_arr['id'], 'author_role', true);
            return empty($val) ? 'Redactor ALAS' : $val;
        }
    ]);

    // 2. Inyectamos el Flag del Ranking
    register_rest_field('post', 'show_ranking', [
        'get_callback' => function($post_arr) {
            $val = get_post_meta($post_arr['id'], 'show_ranking', true);
            // MAGIA AQUÍ: Convertimos forzosamente el dato de la DB a un Boolean estricto de PHP.
            // Si en la DB hay un '1', devolverá TRUE. Si hay cualquier otra cosa, devolverá FALSE.
            return $val === '1';
        }
    ]);
}



// 1. Registra l'interfaccia UI (Meta Box) nell'editor
add_action('add_meta_boxes', 'alas_add_author_role_meta_box');

function alas_add_author_role_meta_box() {
    add_meta_box(
        'alas_author_role_box',       // ID del contenitore HTML
        'Detalles para la Web',   // Titolo visibile ai redattori
        'alas_render_headless_ui', // Funzione di callback che disegna l'HTML
        'post',                       // Si applica solo ai Posts
        'side',                       // Posizione: barra laterale destra
        'high'                        // Priorità di visualizzazione
    );
}

// 2. Disegna l'HTML del campo
function alas_render_headless_ui($post) {
    wp_nonce_field('alas_save_headless_data', 'alas_headless_nonce');

    $current_role = get_post_meta($post->ID, 'author_role', true) ?: 'Redactor ALAS';
    // Recuperamos el booleano (WordPress guarda los booleanos como '1' o '')
    $show_ranking = get_post_meta($post->ID, 'show_ranking', true);

    echo '<label for="author_role" style="font-weight:600; display:block; margin-bottom:4px;">Cargo del Autor:</label>';
    echo '<input type="text" id="author_role" name="author_role" value="' . esc_attr($current_role) . '" style="width:100%; padding: 5px; margin-bottom:15px;" />';

    // UI: Checkbox para el Ranking
    echo '<label style="font-weight:600; display:flex; align-items:center; gap: 8px;">';
    // La función checked() de WP marca el checkbox si el valor en DB es '1'
    echo '<input type="checkbox" name="show_ranking" value="1" ' . checked($show_ranking, '1', false) . ' />';
    echo 'Mostrar widget de Ranking en vivo en esta noticia';
    echo '</label>';
    
    echo '<p style="font-size: 11px; color: #666; margin-top: 15px;">Estos valores viajan por la API hacia la PWA de Angular.</p>';
}

function alas_save_headless_data($post_id) {
    if (!isset($_POST['alas_headless_nonce']) || !wp_verify_nonce($_POST['alas_headless_nonce'], 'alas_save_headless_data')) return;
    if (defined('DOING_AUTOSAVE') && DOING_AUTOSAVE) return;
    if (!current_user_can('edit_post', $post_id)) return;

    if (isset($_POST['author_role'])) {
        update_post_meta($post_id, 'author_role', sanitize_text_field($_POST['author_role']));
    }
    
    // Guardado seguro del Checkbox (Booleano)
    // Si el checkbox está marcado, viene en el POST. Si no está marcado, no viene.
    // EL FIX SENIOR: Guardamos explícitamente '1' o '0' en lugar de true/false.
    // Esto evita que WP guarde strings vacíos ("") que rompen el esquema de la REST API.
    $ranking_flag = isset($_POST['show_ranking']) ? '1' : '0';
    update_post_meta($post_id, 'show_ranking', $ranking_flag);
}

//considerar en C#

using System.Text.RegularExpressions;

namespace Alas.Bff.Application.Helpers;

public static partial class ContentMetricsHelper
{
    // [Senior Tip]: Esto le dice al compilador de Roslyn que genere el código C# óptimo
    // para esta expresión regular, reduciendo a cero el "allocation overhead".
    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagsRegex();

    public static int CalculateReadTime(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return 1;

        // Limpiamos los tags HTML de forma ultra-rápida
        var textOnly = HtmlTagsRegex().Replace(htmlContent, string.Empty);
        
        // Contamos las palabras
        var wordCount = textOnly.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        // 200 palabras por minuto es el estándar de lectura
        var minutes = (int)Math.Ceiling(wordCount / 200.0);
        
        return minutes == 0 ? 1 : minutes;
    }
}

//El Contrato (DTO) para Angular

namespace Alas.Bff.Application.DTOs;

// Actualizamos el objeto Meta
public record WpMeta(
    [property: JsonPropertyName("author_role")] string AuthorRole,
    // C# serializa automáticamente el true/false de la API REST
    [property: JsonPropertyName("show_ranking")] bool ShowRanking 
);

// Y en tu DTO final para Angular:
public record NewsArticleDto(
    int Id,
    string Title,
    string Content,
    string AuthorName,
    string AuthorRole,     
    DateTime PublishedDate,
    int ReadTimeMinutes,
    bool ShowRankingWidget // <-- Angular leerá esto con un *ngIf="article.showRankingWidget"
);

//El Mapeo en tu Servicio (BFF)

public async Task<List<NewsArticleDto>> GetNewsForAngularAsync(CancellationToken ct = default)
{
    // 1. Obtenemos la data cruda de WP (asumiendo que ya tienes el método GetNewsAsync)
    var rawPosts = await _wordPressService.GetNewsAsync(ct);

    // 2. Mapeamos y calculamos
    var cleanNews = rawPosts.Select(wp => new NewsArticleDto(
        Id: wp.Id,
        Title: wp.Title.Rendered,
        Content: wp.Content.Rendered,
        AuthorName: wp.Embedded?.Author?.FirstOrDefault()?.Name ?? "Equipo ALAS",
        AuthorRole: wp.Meta?.AuthorRole ?? "Redactor",
        PublishedDate: wp.Date,
        ReadTimeMinutes: ContentMetricsHelper.CalculateReadTime(wp.Content.Rendered)
    )).ToList();

    return cleanNews;
}

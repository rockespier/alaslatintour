<?php
/**
 * Twenty Twenty-Five functions and definitions.
 *
 * @link https://developer.wordpress.org/themes/basics/theme-functions/
 *
 * @package WordPress
 * @subpackage Twenty_Twenty_Five
 * @since Twenty Twenty-Five 1.0
 */

if ( ! function_exists( 'twentytwentyfive_post_format_setup' ) ) :
	/**
	 * Adds theme support for post formats.
	 *
	 * @since Twenty Twenty-Five 1.0
	 *
	 * @return void
	 */
	function twentytwentyfive_post_format_setup() {
		add_theme_support( 'post-formats', array( 'aside', 'audio', 'chat', 'gallery', 'image', 'link', 'quote', 'status', 'video' ) );
	}
endif;
add_action( 'after_setup_theme', 'twentytwentyfive_post_format_setup' );

if ( ! function_exists( 'twentytwentyfive_editor_style' ) ) :
	/**
	 * Enqueues editor-style.css in the editors.
	 *
	 * @since Twenty Twenty-Five 1.0
	 *
	 * @return void
	 */
	function twentytwentyfive_editor_style() {
		add_editor_style( 'assets/css/editor-style.css' );
	}
endif;
add_action( 'after_setup_theme', 'twentytwentyfive_editor_style' );

if ( ! function_exists( 'twentytwentyfive_enqueue_styles' ) ) :
	/**
	 * Enqueues the theme stylesheet on the front.
	 *
	 * @since Twenty Twenty-Five 1.0
	 *
	 * @return void
	 */
	function twentytwentyfive_enqueue_styles() {
		$suffix = SCRIPT_DEBUG ? '' : '.min';
		$src    = 'style' . $suffix . '.css';

		wp_enqueue_style(
			'twentytwentyfive-style',
			get_parent_theme_file_uri( $src ),
			array(),
			wp_get_theme()->get( 'Version' )
		);
		wp_style_add_data(
			'twentytwentyfive-style',
			'path',
			get_parent_theme_file_path( $src )
		);
	}
endif;
add_action( 'wp_enqueue_scripts', 'twentytwentyfive_enqueue_styles' );

if ( ! function_exists( 'twentytwentyfive_block_styles' ) ) :
	/**
	 * Registers custom block styles.
	 *
	 * @since Twenty Twenty-Five 1.0
	 *
	 * @return void
	 */
	function twentytwentyfive_block_styles() {
		register_block_style(
			'core/list',
			array(
				'name'         => 'checkmark-list',
				'label'        => __( 'Checkmark', 'twentytwentyfive' ),
				'inline_style' => '
				ul.is-style-checkmark-list {
					list-style-type: "\2713";
				}

				ul.is-style-checkmark-list li {
					padding-inline-start: 1ch;
				}',
			)
		);
	}
endif;
add_action( 'init', 'twentytwentyfive_block_styles' );

if ( ! function_exists( 'twentytwentyfive_pattern_categories' ) ) :
	/**
	 * Registers pattern categories.
	 *
	 * @since Twenty Twenty-Five 1.0
	 *
	 * @return void
	 */
	function twentytwentyfive_pattern_categories() {

		register_block_pattern_category(
			'twentytwentyfive_page',
			array(
				'label'       => __( 'Pages', 'twentytwentyfive' ),
				'description' => __( 'A collection of full page layouts.', 'twentytwentyfive' ),
			)
		);

		register_block_pattern_category(
			'twentytwentyfive_post-format',
			array(
				'label'       => __( 'Post formats', 'twentytwentyfive' ),
				'description' => __( 'A collection of post format patterns.', 'twentytwentyfive' ),
			)
		);
	}
endif;
add_action( 'init', 'twentytwentyfive_pattern_categories' );

if ( ! function_exists( 'twentytwentyfive_register_block_bindings' ) ) :
	/**
	 * Registers the post format block binding source.
	 *
	 * @since Twenty Twenty-Five 1.0
	 *
	 * @return void
	 */
	function twentytwentyfive_register_block_bindings() {
		register_block_bindings_source(
			'twentytwentyfive/format',
			array(
				'label'              => _x( 'Post format name', 'Label for the block binding placeholder in the editor', 'twentytwentyfive' ),
				'get_value_callback' => 'twentytwentyfive_format_binding',
			)
		);
	}
endif;
add_action( 'init', 'twentytwentyfive_register_block_bindings' );

if ( ! function_exists( 'twentytwentyfive_format_binding' ) ) :
	/**
	 * Callback function for the post format name block binding source.
	 *
	 * @since Twenty Twenty-Five 1.0
	 *
	 * @return string|void Post format name, or nothing if the format is 'standard'.
	 */
	function twentytwentyfive_format_binding() {
		$post_format_slug = get_post_format();

		if ( $post_format_slug && 'standard' !== $post_format_slug ) {
			return get_post_format_string( $post_format_slug );
		}
	}
endif;
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
		
	register_post_meta('post', 'show_ranking', [
		'show_in_rest' => true,
		'single'       => true,
		'type'         => 'boolean',
		'default'      => false
	]);
	register_post_meta('post', 'featured', [
		'show_in_rest' => true,
		'single'       => true,
		'type'         => 'boolean',
		'default'      => false
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
    $show_ranking = get_post_meta($post->ID, 'show_ranking', true);
    // AGREGADO: Obtenemos el valor guardado de featured
    $featured = get_post_meta($post->ID, 'featured', true); 

    echo '<label for="author_role" style="font-weight:600; display:block; margin-bottom:4px;">Cargo del Autor:</label>';
    echo '<input type="text" id="author_role" name="author_role" value="' . esc_attr($current_role) . '" style="width:100%; padding: 5px; margin-bottom:15px;" />';

    // UI: Checkbox para el Ranking
    echo '<label style="font-weight:600; display:flex; align-items:center; gap: 8px;">';
    echo '<input type="checkbox" name="show_ranking" value="1" ' . checked($show_ranking, '1', false) . ' />';
    echo 'Mostrar widget de Ranking en vivo en esta noticia';
    echo '</label>';
    
    // UI: Checkbox para Feature (Usando la variable $featured correcta)
    echo '<label style="font-weight:600; display:flex; align-items:center; gap: 8px; margin-top: 10px;">';
    echo '<input type="checkbox" name="featured" value="1" ' . checked($featured, '1', false) . ' />';
    echo 'Mostrar como una noticia destacada';
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
	
	$feature_flag = isset($_POST['featured']) ? '1' : '0';
    update_post_meta($post_id, 'featured', $feature_flag);
}
add_action('init', 'alas_register_gallery_post_type');

function alas_register_gallery_post_type() {
    register_post_type('gallery', [
        'labels' => ['name' => 'Galerías', 'singular_name' => 'Galería'],
        'public' => true,
        'has_archive' => true,
        'show_in_rest' => true, // ¡CRÍTICO! Esto hace que ACF y el CPT funcionen con la API
        'supports' => ['title', 'editor', 'thumbnail'],
        'menu_icon' => 'dashicons-format-gallery',
    ]);
}
add_filter('rest_prepare_gallery', 'alas_expand_gallery_acf_images', 10, 3);

function alas_expand_gallery_acf_images($response, $post, $request) {
    // 1. Obtenemos los datos de ACF
    $data = $response->get_data();
    
    // 2. Verificamos si existe el campo repetidor 'gallery_days'
    if (isset($data['acf']['gallery_days']) && is_array($data['acf']['gallery_days'])) {
        
        foreach ($data['acf']['gallery_days'] as &$day) {
            // Verificamos si hay fotos
            if (isset($day['photos']) && is_array($day['photos'])) {
                $expanded_photos = [];
                
                foreach ($day['photos'] as $photo_id) {
                    // 3. Convertimos el ID a objeto completo
                    $image_src = wp_get_attachment_image_src($photo_id, 'full'); // Puedes cambiar 'full' a 'large'
                    $expanded_photos[] = [
                        'id' => $photo_id,
                        'url' => $image_src ? $image_src[0] : null,
                        'width' => $image_src ? $image_src[1] : null,
                        'height' => $image_src ? $image_src[2] : null
                    ];
                }
                // Reemplazamos el array de IDs por el array de Objetos
                $day['photos'] = $expanded_photos;
            }
        }
        
        // 4. Guardamos los datos modificados de vuelta en la respuesta
        $response->set_data($data);
    }
    
    return $response;
}

function mi_custom_login_logo() {
    ?>
    <style type="text/css">
        #login h1 a, .login h1 a {
            background-image: url('https://www.alasglobaltour.rtres.net/wp-content/uploads/2026/07/new-logo-blue-4x.png');
            height: 80px; /* Ajusta según la altura de tu logo */
            width: 320px; /* Ajusta según el ancho de tu logo */
            background-size: contain;
            background-repeat: no-repeat;
            padding-bottom: 30px;
        }
    </style>
    <?php
}
add_action( 'login_enqueue_scripts', 'mi_custom_login_logo' );

add_action('save_post', 'alas_save_headless_data');

// Fuerza el filtro por idioma en la REST API para 'post' (Noticias), 'gallery' (Fotos) y 'page' (Quiénes somos)
add_filter('rest_post_query', 'alas_apply_lang_filter', 10, 2);
add_filter('rest_gallery_query', 'alas_apply_lang_filter', 10, 2);
add_filter('rest_page_query', 'alas_apply_lang_filter', 10, 2);

function alas_apply_lang_filter($args, $request) {
    $lang = $request->get_param('lang');
    if (!empty($lang) && function_exists('pll_languages_list')) {
        $available = pll_languages_list(); // ej: ['es','en','pt']
        if (in_array($lang, $available, true)) {
            $args['lang'] = sanitize_text_field($lang);
        }
    }
    return $args;
}

// Expone en el JSON el idioma de cada post (pll_lang, solo informativo) y el slug de sus
// traducciones { "es": "...", "en": "...", "pt": "..." } (translations, lo usa el backend .NET
// para el selector de idioma y los hreflang de noticias/galerías).
add_action('rest_api_init', function () {
    foreach (['post', 'gallery', 'page'] as $post_type) {
        register_rest_field($post_type, 'pll_lang', [
            'get_callback' => function ($post_arr) {
                return function_exists('pll_get_post_language')
                    ? pll_get_post_language($post_arr['id'], 'slug')
                    : null;
            },
            'schema' => ['type' => 'string', 'context' => ['view', 'embed']],
        ]);

        register_rest_field($post_type, 'translations', [
            'get_callback' => function ($post_arr) {
                $slugs = [];
                if (function_exists('pll_get_post_translations')) {
                    foreach (pll_get_post_translations($post_arr['id']) as $lang => $id) {
                        if (get_post_status($id) === 'publish') {
                            $slugs[$lang] = get_post_field('post_name', $id);
                        }
                    }
                }
                return (object) $slugs; // (object): siempre {} y nunca [] cuando está vacío
            },
            'schema' => ['type' => 'object', 'context' => ['view', 'embed']],
        ]);
    }
});


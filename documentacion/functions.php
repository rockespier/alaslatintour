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
	
	// UI: Checkbox para Feature
    echo '<label style="font-weight:600; display:flex; align-items:center; gap: 8px;">';
    // La función checked() de WP marca el checkbox si el valor en DB es '1'
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

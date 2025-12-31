-- Actualizar usuario admin: confirmar email
UPDATE public.users 
SET email_confirmed = true 
WHERE email = 'admin@admin.com';

-- Verificar actualización
SELECT 
    u.id,
    u.email,
    u.full_name,
    u.enabled,
    u.email_confirmed,
    r.name as role
FROM 
    public.users u
    LEFT JOIN public.user_roles ur ON u.id = ur.user_id
    LEFT JOIN public.roles r ON ur.role_id = r.id
WHERE 
    u.email = 'admin@admin.com';

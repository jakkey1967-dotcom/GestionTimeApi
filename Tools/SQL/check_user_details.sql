-- Verificar usuario específico
SELECT 
    u.id,
    u.email,
    u.full_name,
    u.enabled,
    u.email_confirmed,
    u.must_change_password,
    u.password_changed_at,
    u.password_expiration_days,
    r.name as role
FROM 
    public.users u
    LEFT JOIN public.user_roles ur ON u.id = ur.user_id
    LEFT JOIN public.roles r ON ur.role_id = r.id
WHERE 
    u.email = 'msn@tdkortal.com';

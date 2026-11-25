using Microsoft.EntityFrameworkCore;
using WS_APIPAISES_DOTNET_REST.Data;
using WS_APIPAISES_DOTNET_REST.Models.Entidades;

namespace WS_APIPAISES_DOTNET_REST.DAO;

/// <summary>Acceso a la tabla Usuarios (ECUD1, paso 5).</summary>
public class UsuarioDAO
{
    private readonly AppDbContext db;

    public UsuarioDAO(AppDbContext db)
    {
        this.db = db;
    }

    /// <summary>Usuario registrado con ese nombre de usuario, o null si no existe.</summary>
    public Task<Usuario?> BuscarPorUsuarioAsync(string usuario)
    {
        return db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.NombreUsuario == usuario);
    }
}

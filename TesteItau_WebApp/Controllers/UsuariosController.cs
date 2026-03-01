using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using TesteItau_WebApp.DAO;
using TesteItau_WebApp.Models;

namespace TesteItau_WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UsuariosController : Controller
	{
		private readonly AppDBContext _context;

		public UsuariosController(AppDBContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<IActionResult> PostUsuario([FromBody] Usuario usuario)
		{
			var cliente = await _context.Clientes
				.FirstOrDefaultAsync(c => c.Id == usuario.ClienteId);

			if (cliente == null)
				return BadRequest("ClienteId informado não existe.");

			var jaExiste = await _context.Usuarios
				.AnyAsync(u => u.ClienteId == usuario.ClienteId);

			if (jaExiste)
				return BadRequest("Já existe usuário para este Cliente.");

			if (usuario.Tipo != "CLIENTE" && usuario.Tipo != "ADMINISTRADOR")
			{
				return BadRequest("Tipo deve ser 'CLIENTE' ou 'ADMINISTRADOR'.");
			}

			if (string.IsNullOrWhiteSpace(usuario.Senha))
				return BadRequest("Senha é obrigatória.");

			usuario.Id = 0;

			usuario.Email = cliente.Email;

			usuario.Senha = GerarHash(usuario.Senha);

			_context.Usuarios.Add(usuario);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetUsuario),
				new { id = usuario.Id }, usuario);
		}

		[HttpGet]
		public async Task<IActionResult> GetUsuarios()
		{
			var usuarios = await _context.Usuarios.ToListAsync();
			return Ok(usuarios);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetUsuario(long id)
		{
			var usuario = await _context.Usuarios
				.FirstOrDefaultAsync(u => u.Id == id);

			if (usuario == null)
				return NotFound();

			return Ok(usuario);
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var usuario = await _context.Usuarios
				.FirstOrDefaultAsync(u => u.Email == request.Email);

			if (usuario == null)
				return Unauthorized("Usuário ou senha inválidos.");

			var senhaHash = GerarHash(request.Senha);

			if (usuario.Senha != senhaHash)
				return Unauthorized("Usuário ou senha inválidos.");

			return Ok(new
			{
				usuario.Id,
				usuario.Email,
				usuario.Tipo
			});
		}

		public class LoginRequest
		{
			public string Email { get; set; } = null!;
			public string Senha { get; set; } = null!;
		}

		private string GerarHash(string senha)
		{
			using var sha = SHA256.Create();
			var bytes = Encoding.UTF8.GetBytes(senha);
			var hash = sha.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}
	}
}

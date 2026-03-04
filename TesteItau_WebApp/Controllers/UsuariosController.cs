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

        /// <summary>
        /// Adiciona um novo Usuário a tabela, é necessário adicionar o usuário para ter acesso ao Login
        /// </summary>
        /// <param name="usuario"> Recebe um objeto Usuario com, clienteId, email, senha e tipo</param>
        /// <returns>Informações da compra gerada</returns>
        /// <response code="200">Usuario gerado com sucesso.</response>
        /// <response code="400">Falha ao gerar usuario, variáveis incorretas</response>
        [HttpPost]
		public async Task<IActionResult> PostUsuario([FromBody] Usuario usuario)
		{
			//Validando o ClienteId
			var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == usuario.ClienteId);
			if (cliente == null)
				return BadRequest("ClienteId informado não existe.");

			//Validando se usuário já existe, para manter relação 1:1
			var jaExiste = await _context.Usuarios.AnyAsync(u => u.ClienteId == usuario.ClienteId);
			if (jaExiste)
				return BadRequest("Já existe usuário para este Cliente.");

			//Validando o tipo, isso muda também o acesso na interface WEB
			if (usuario.Tipo != "CLIENTE" && usuario.Tipo != "ADMINISTRADOR")
			{
				return BadRequest("Tipo deve ser 'CLIENTE' ou 'ADMINISTRADOR'.");
			}

			//Validando senha em branco
			if (string.IsNullOrWhiteSpace(usuario.Senha))
				return BadRequest("Senha é obrigatória.");

			usuario.Id = 0;

			usuario.Email = cliente.Email;

			//Senhas são criptografadas por Hash no banco, para não salvar as senhas em plainText
			usuario.Senha = GerarHash(usuario.Senha);

			_context.Usuarios.Add(usuario);
			await _context.SaveChangesAsync();

			return CreatedAtAction(nameof(GetUsuario),new { id = usuario.Id }, usuario);
		}

        /// <summary>
        /// Retorna Json com todos os usuários
        /// </summary>
        /// <returns>Informações de todos os usuários</returns>
        /// <response code="200">Usuario gerado com sucesso.</response>
        [HttpGet]
		public async Task<IActionResult> GetUsuarios()
		{
			var usuarios = await _context.Usuarios.ToListAsync();
			return Ok(usuarios);
		}

        /// <summary>
        /// Retorna informações de usuário dado certo ID
        /// </summary>
        /// <param name="id"> Id do usuario</param>
        /// <returns>Informacoes do usuario</returns>
        /// <response code="200">Usuario retornado com sucesso.</response>
        /// <response code="400">Falha ao retornar usuario, variáveis incorretas</response>
        [HttpGet("{id}")]
		public async Task<IActionResult> GetUsuario(long id)
		{
			var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

			if (usuario == null)
				return NotFound();

			return Ok(usuario);
		}

        /// <summary>
        /// Rota de Login
        /// </summary>
        /// <param name="request"> Recebe um objeto Login comemail e senha</param>
        /// <returns>Ok Login</returns>
        /// <response code="200">Login feito com sucesso.</response>
        /// <response code="400">Falha ao realizar login, variáveis incorretas</response>
        [HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

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

		//Aqui eu gero a string Hash para manter de cookie, será utilizada para acessos a conta e para validar o ClienteId durante a operacao do site
		private string GerarHash(string senha)
		{
			using var sha = SHA256.Create();
			var bytes = Encoding.UTF8.GetBytes(senha);
			var hash = sha.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}
	}
}

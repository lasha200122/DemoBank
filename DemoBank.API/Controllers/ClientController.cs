using AutoMapper;
using DemoBank.API.Services;
using DemoBank.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoBank.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ClientController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly IMapper _mapper;

        public ClientController(IClientService clientService, IMapper mapper)
        {
            _clientService = clientService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<List<AdminClientListDto>> GetClientList()
        {
            var clients = await _clientService.GetClientList();

            var clientDtos = _mapper.Map<List<AdminClientListDto>>(clients);

            return clientDtos;
        }
    }
}

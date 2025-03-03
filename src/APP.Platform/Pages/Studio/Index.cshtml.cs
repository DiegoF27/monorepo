using System.Text;
using Domain.Entities;
using Domain.Models;
using Domain.Models.ViewModels;
using Domain.WebServices;
using Infrastructure.Data.Contexts;
using Microsoft.AspNetCore.Mvc;
using Platform.Services;
using Queue;

namespace APP.Platform.Pages.Studio
{
    public sealed class StudioModel : CustomPageModel
    {
        private new readonly ApplicationDbContext _context;
        private readonly RateLimit _rateLimit;
        private ILiveWebService _webservice { get; set; }

        public Live? Live { get; set; }

        [BindProperty]
        public List<Domain.Entities.Perfil>? Perfil { get; set; }

        public StudioModel(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            Settings settings,
            RateLimit rateLimit,
            ILiveWebService webservice
        )
            : base(context, httpClientFactory, httpContextAccessor, settings)
        {
            _context = context;
            _rateLimit = rateLimit;
            _webservice = webservice;
        }

        public async Task<IActionResult> OnGetAsync(Guid mainkey)
        {
            if (IsAuthenticatedWithoutProfile())
            {
                return Redirect("../Perfil");
            }

            var liveCore = await _webservice.GetLiveById(mainkey);
            if (liveCore == null)
            {
                return Redirect("../Index");
            }

            var live = new Live
            {
                Titulo = liveCore.Titulo,
                Descricao = liveCore.Descricao,
                DataCriacao = liveCore.DataCriacao,
                Thumbnail = liveCore.Thumbnail
            };
            Live = live;

            return Page();
        }

        public ActionResult OnPostDeleteMessage(string messageId, string liveId)
        {
            if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(liveId))
            {
                return BadRequest();
            }

            var message = _context.ChatMessages.FirstOrDefault(x => x.Id == Guid.Parse(messageId));

            if (message == null)
            {
                return BadRequest();
            }

            if (message.PerfilId != UserProfile.Id)
            {
                return BadRequest();
            }

            message.Valid = false;
            _context.ChatMessages.Update(message);
            _context.SaveChanges();

            // _messagingAdapter.Publish(
            //     "removeChatMessage",
            //     Encoding.UTF8.GetBytes(
            //         JsonConvert.SerializeObject(new { LiveId = liveId, MessageId = messageId })
            //     )
            // );
            return new JsonResult(new { LiveId = liveId, MessageId = messageId });
        }

        public ActionResult OnPostMessage(string comment, string liveId)
        {
            if (string.IsNullOrEmpty(comment) || string.IsNullOrEmpty(liveId))
            {
                return BadRequest();
            }

            if (_rateLimit.IsRateLimited(UserProfile.Id.ToString(), liveId))
            {
                return BadRequest("Você está enviando mensagens muito rápido.");
            }

            var formatedDate = string.Empty;
            var messageToProcess = new ChatMessageRequestModel
            {
                Id = Guid.NewGuid(),
                PerfilId = UserProfile.Id,
                LiveId = Guid.Parse(liveId),
                Content = comment,
                DataCriacao = DateTime.Now,
                Foto = UserProfile.Foto,
                Nome = UserProfile.Nome,
                Data = formatedDate
            };

            // var message = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageToProcess));
            // _messagingAdapter.Publish("chatMessage", message);
            return new JsonResult(new { });
        }
    }
}

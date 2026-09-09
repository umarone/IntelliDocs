using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Documents;
using AIChatAssistant.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIChatAssistant.Controllers
{
    [Route("api/documents")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentProcessingService _documentService;
        private readonly IDocumentRepository _documentRepository;
        public DocumentController(IDocumentProcessingService documentService, IDocumentRepository documentRepository)
        {
            _documentService = documentService;
            _documentRepository = documentRepository;
        }
        [ HttpPost("upload")]
public async Task<IActionResult> Upload(
    IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            using var memoryStream = new MemoryStream();

            await file.CopyToAsync(memoryStream);

            var document = new Document
            {
                DocumentId = Guid.NewGuid(),
                FileName = file.FileName,
                Content = memoryStream.ToArray()
            };
            
            await _documentService.ProcessAsync(document);
            return Ok("Document indexed successfully.");
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _documentRepository.GetAllAsync();

            var result = documents.Select(d => new DocumentSummaryDto
            {
                DocumentId = d.DocumentId,
                FileName = d.FileName,
                ContentType = d.ContentType,
                UploadedAt = d.UploadedAt
            });

            return Ok(result);
        }
    }
}

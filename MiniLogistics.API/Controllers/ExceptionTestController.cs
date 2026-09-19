using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.Exceptions;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/exception-test")]
public class ExceptionTestController : ControllerBase
{
    [HttpGet("400")]
    public IActionResult Test400()
    {
        throw new BadRequestException(
            "Dữ liệu request không hợp lệ."
        );
    }

    [HttpGet("401")]
    public IActionResult Test401()
    {
        throw new UnauthorizedException(
            "Bạn chưa đăng nhập."
        );
    }

    [HttpGet("403")]
    public IActionResult Test403()
    {
        throw new ForbiddenException(
            "Bạn không có quyền thực hiện thao tác này."
        );
    }

    [HttpGet("404")]
    public IActionResult Test404()
    {
        throw new NotFoundException(
            "Không tìm thấy dữ liệu."
        );
    }

    [HttpGet("500")]
    public IActionResult Test500()
    {
        throw new Exception(
            "Lỗi server giả lập."
        );
    }
}
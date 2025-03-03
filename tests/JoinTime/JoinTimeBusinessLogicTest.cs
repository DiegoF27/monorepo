using Application.Logic;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Moq;
using Queue;

namespace tests;

public class JoinTimeBusinessLogicTest
{
    private readonly Mock<ITimeSelectionRepository> mockTimeSelectionRepository;
    private readonly Mock<IJoinTimeRepository> mockJoinTimeRepository;
    private readonly Mock<IMessagePublisher> mockMessagePublisher;

    public JoinTimeBusinessLogicTest()
    {
        mockTimeSelectionRepository = new Mock<ITimeSelectionRepository>();
        mockJoinTimeRepository = new Mock<IJoinTimeRepository>();
        mockMessagePublisher = new Mock<IMessagePublisher>();
    }

    [Fact]
    public async Task UpdateOldJoinTimes_ShouldUpdateStatusAndNotify_WhenVariacaoIsOneToOne()
    {
        // Arrange
        var joinTimeBusinessLogic = new JoinTimeBusinessLogic(
            mockTimeSelectionRepository.Object,
            mockJoinTimeRepository.Object,
            mockMessagePublisher.Object
        );
        var perfilTimeSelection = Guid.NewGuid();

        var timeSelection = TimeSelection.Create(
            perfilTimeSelection,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            EnumTipoTimeSelection.Live,
            TipoAction.Ensinar,
            Variacao.OneToOne
        );

        var joinTime = JoinTime.Create(
            Guid.NewGuid(),
            timeSelection.Id,
            StatusJoinTime.Marcado,
            false,
            TipoAction.Aprender
        );

        var oldFreeTimes = new Dictionary<JoinTime, TimeSelection> { { joinTime, timeSelection } };

        mockJoinTimeRepository
            .Setup(repo => repo.GetFreeTimeMarcadosAntigos())
            .ReturnsAsync(oldFreeTimes);
        mockTimeSelectionRepository
            .Setup(repo => repo.GetTimeSelectionPerfilIdsByJoinTimeIds(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, Guid> { { timeSelection.Id, perfilTimeSelection } });

        // Act
        await joinTimeBusinessLogic.UpdateOldJoinTimes();

        // Assert
        mockJoinTimeRepository.Verify(
            repo => repo.UpdateRange(It.IsAny<List<JoinTime>>()),
            Times.Once
        );
        mockMessagePublisher.Verify(
            pub => pub.PublishAsync("NotificationsQueue", It.IsAny<Notification>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateOldJoinTimes_ShouldOnlyUpdateStatus_WhenVariacaoIsNotOneToOne()
    {
        // Arrange
        var joinTimeBusinessLogic = new JoinTimeBusinessLogic(
            mockTimeSelectionRepository.Object,
            mockJoinTimeRepository.Object,
            mockMessagePublisher.Object
        );

        var timeSelection = TimeSelection.Create(
            Guid.NewGuid(),
            null,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            EnumTipoTimeSelection.Live,
            TipoAction.Ensinar,
            Variacao.CursoOuEvento
        );

        var joinTime = JoinTime.Create(
            Guid.NewGuid(),
            timeSelection.Id,
            StatusJoinTime.Marcado,
            false,
            TipoAction.Aprender
        );

        var oldFreeTimes = new Dictionary<JoinTime, TimeSelection> { { joinTime, timeSelection } };

        mockJoinTimeRepository
            .Setup(repo => repo.GetFreeTimeMarcadosAntigos())
            .ReturnsAsync(oldFreeTimes);

        // Act
        await joinTimeBusinessLogic.UpdateOldJoinTimes();

        // Assert
        mockJoinTimeRepository.Verify(
            repo => repo.UpdateRange(It.IsAny<List<JoinTime>>()),
            Times.Once
        );
        mockMessagePublisher.Verify(
            pub => pub.PublishAsync("NotificationsQueue", It.IsAny<Notification>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateOldJoinTimes_ShouldHandleEmptyOldFreeTimes()
    {
        // Arrange
        var joinTimeBusinessLogic = new JoinTimeBusinessLogic(
            mockTimeSelectionRepository.Object,
            mockJoinTimeRepository.Object,
            mockMessagePublisher.Object
        );

        mockJoinTimeRepository
            .Setup(repo => repo.GetFreeTimeMarcadosAntigos())
            .ReturnsAsync(new Dictionary<JoinTime, TimeSelection>());

        // Act
        await joinTimeBusinessLogic.UpdateOldJoinTimes();

        // Assert
        mockJoinTimeRepository.Verify(
            repo => repo.UpdateRange(It.IsAny<List<JoinTime>>()),
            Times.Once
        );
        mockMessagePublisher.Verify(
            pub => pub.PublishAsync("NotificationsQueue", It.IsAny<Notification>()),
            Times.Never
        );
    }
}

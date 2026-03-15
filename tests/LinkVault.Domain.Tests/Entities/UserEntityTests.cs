using FluentAssertions;
using LinkVault.Domain.Abstractions;
using LinkVault.Domain.Entities;
using LinkVault.Domain.Exceptions;

namespace LinkVault.Domain.Tests.Entities;

public class UserEntityTests
{
    private static UserEntity CreateUser(
        string emailEncrypted = "encrypted-email",
        string emailBlindIndexHash = "blind-index-email",
        string FirstNameEncrypted = "encrypted-name",
        string SurNameEncrypted = "encrypted-surname",
        string passwordHash = "hash123",
        string encryptedDek = "encrypted-dek")
        => UserEntity.Register(
            emailEncrypted, 
            emailBlindIndexHash, 
            FirstNameEncrypted,
            SurNameEncrypted,
            passwordHash, 
            encryptedDek
        );

    [Fact]
    public void Register_ShouldInitializePropertiesCorrectly()
    {
        var user = CreateUser();

        user.Id.Should().NotBe(Guid.Empty);
        user.EmailEncrypted.Should().Be("encrypted-email");
        user.EmailBlindIndexHash.Should().Be("blind-index-email");
        user.FirstNameEncrypted.Should().Be("encrypted-name");
        user.SurNameEncrypted.Should().Be("encrypted-surname");
        user.PasswordHash.Should().Be("hash123");
        user.EncryptedDek.Should().Be("encrypted-dek");
        user.EmailConfirmed.Should().BeFalse();
        user.IsDeleted.Should().BeFalse();
        user.PendingEmailEncrypted.Should().BeNull();
        user.PendingEmailBlindIndexHash.Should().BeNull();
        user.UpdatedAt.Should().BeNull();
        user.LastLogin.Should().BeNull();
    }

    [Fact]
    public void Register_ShouldInheritFromBaseEntity()
    {
        var user = CreateUser();
        user.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void Update_ShouldBeUpdated()
    {
        var user = CreateUser();
        user.Update();
        user.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenEmailEncryptedIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => UserEntity.Register(value, "blind", "name", "surname", "hash", "dek");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenEmailBlindIndexIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => UserEntity.Register("email", value, "name", "surname", "hash", "dek");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenDisplayNameEncryptedIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => UserEntity.Register("email", "blind", value,"surname", "hash", "dek");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenSurNameEncryptedIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => UserEntity.Register("email", "blind", "name",value, "hash", "dek");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenPasswordHashIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => UserEntity.Register("email", "blind", "name","surname", value, "dek");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenEncryptedDekIsEmpty_ShouldThrowArgumentException(string value)
    {
        var act = () => UserEntity.Register("email", "blind", "name","surname", "hash", value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConfirmEmail_WhenNotYetConfirmed_ShouldSetEmailConfirmedTrue()
    {
        var user = CreateUser();

        user.ConfirmEmail();

        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ConfirmEmail_WhenAlreadyConfirmed_ShouldThrowEmailAlreadyConfirmedException()
    {
        var user = CreateUser();
        user.ConfirmEmail();

        var act = () => user.ConfirmEmail();

        act.Should().Throw<EmailAlreadyConfirmedException>();
    }

    [Fact]
    public void RequestEmailChange_ShouldSetPendingEmailFields()
    {
        var user = CreateUser();

        user.RequestEmailChange("new-encrypted-email", "new-blind-index");

        user.PendingEmailEncrypted.Should().Be("new-encrypted-email");
        user.PendingEmailBlindIndexHash.Should().Be("new-blind-index");
    }

    [Fact]
    public void RequestEmailChange_WhenBlindIndexSameAsCurrent_ShouldThrowArgumentException()
    {
        var user = CreateUser(emailBlindIndexHash: "same-blind-index");

        var act = () => user.RequestEmailChange("any-encrypted", "same-blind-index");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RequestEmailChange_WhenEncryptedEmailIsEmpty_ShouldThrowArgumentException(string value)
    {
        var user = CreateUser();

        var act = () => user.RequestEmailChange(value, "new-blind-index");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RequestEmailChange_WhenBlindIndexIsEmpty_ShouldThrowArgumentException(string value)
    {
        var user = CreateUser();

        var act = () => user.RequestEmailChange("new-encrypted", value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ConfirmEmailChange_WhenPendingEmailExists_ShouldPromotePendingToCurrentAndClear()
    {
        var user = CreateUser(emailBlindIndexHash: "old-blind");
        user.RequestEmailChange("new-encrypted-email", "new-blind-index");

        user.ConfirmEmailChange();

        user.EmailEncrypted.Should().Be("new-encrypted-email");
        user.EmailBlindIndexHash.Should().Be("new-blind-index");
        user.PendingEmailEncrypted.Should().BeNull();
        user.PendingEmailBlindIndexHash.Should().BeNull();
    }

    [Fact]
    public void ConfirmEmailChange_WhenNoPendingEmail_ShouldThrowInvalidOperationException()
    {
        var user = CreateUser();

        var act = () => user.ConfirmEmailChange();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ChangePassword_ShouldUpdatePasswordHash()
    {
        var user = CreateUser();

        user.ChangePassword("new-hash-456");

        user.PasswordHash.Should().Be("new-hash-456");
    }

    [Fact]
    public void ChangePassword_WhenNewHashIsSameAsOld_ShouldThrowArgumentException()
    {
        var user = CreateUser();

        var act = () => user.ChangePassword("hash123");

        act.Should().Throw<SamePasswordException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangePassword_WhenNewHashIsEmpty_ShouldThrowArgumentException(string value)
    {
        var user = CreateUser();

        var act = () => user.ChangePassword(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateDisplayNameEncrypted()
    {
        var user = CreateUser();

        user.UpdateProfile("new-encrypted-name", "new-encrypted-surname");

        user.FirstNameEncrypted.Should().Be("new-encrypted-name");
        user.SurNameEncrypted.Should().Be("new-encrypted-surname");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WhenEncryptedNameIsEmpty_ShouldThrowArgumentException(string value)
    {
        var user = CreateUser();

        var act = () => user.UpdateProfile(value,value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Delete_ShouldMarkUserAsDeleted()
    {
        var user = CreateUser();

        user.Delete();

        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Delete_WhenCalledTwice_ShouldNotChangeDeletedAt()
    {
        var user = CreateUser();
        user.Delete();
        var firstDeletedAt = user.DeletedAt;

        user.Delete();

        user.DeletedAt.Should().Be(firstDeletedAt);
    }
}
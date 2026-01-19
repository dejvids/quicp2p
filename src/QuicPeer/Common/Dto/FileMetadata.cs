namespace QuicPeer.Common.Dto;

public record FileMetadata(string FileName, long FileSize, string Checksum, long DataStreamId = 0);
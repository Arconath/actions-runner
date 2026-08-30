# Arconath maintained patch queue

- Upstream tag: `v2.337.0`
- Upstream commit: `397b032cbf865e9c3ddfab89d533ec19325e1273`
- Owner: Arconath platform/CI maintainers
- Sync: weekly upstream comparison and reviewed monthly rebase
- Security SLA: critical assessment within 24 hours and patch or runner disable
  within 72 hours; high assessment within 72 hours and patch within seven days

## credential-seal

The runner consumes JIT configuration from a bounded one-shot file rather than
an argument or secret-bearing environment variable. Linux `openat2` resolves
the file beneath the pinned job root without symlinks; `statx` requires a
runner-owned mode-0600 regular inode with one link. The opened inode is unlinked
before a bounded read, with post-read size/inode checks rejecting replacement
or growth races. Only the three pinned v2.337.0 configuration filenames are
accepted. After the authenticated session
is created, credential and RSA files are unlinked before the listener accepts a
job. Credential data and RSA parameters remain in listener memory for token
refresh and reconnect. Ephemeral configuration refresh/session restart is
rejected because it could recreate same-UID-readable configuration.

Compatibility requires Linux-x64 layout build, focused unsafe-file and
credential-order L0 security tests, a real
JIT job, credential-file absence before `Listening for Jobs`, no JIT secret in
argv/environment, reconnect evidence, and complete cgroup/filesystem/runner
registration cleanup. Roll back only to an earlier reviewed Arconath build; the
unpatched upstream v2.337.0 binary does not provide this boundary.

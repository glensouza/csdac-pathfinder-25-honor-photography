### Workflow Fails with Sudo Password Required

If you see "sudo: a terminal is required to read the password" or "sudo: a password is required":

1. Check that the sudoers file exists and has correct syntax:
   ```bash
   sudo cat /etc/sudoers.d/pathfinder
   sudo visudo -c -f /etc/sudoers.d/pathfinder
   ```

2. Verify the pathfinder user exists and has the correct permissions:
   ```bash
   id pathfinder
   sudo -u pathfinder sudo -l
   ```

3. Ensure all required systemctl subcommands are in the sudoers file (start, stop, restart, status, is-active, reload)

4. If issues persist, recreate the sudoers file following Step 7.2 exactly

### Workflow Fails

Check the Actions tab in GitHub for detailed error messages and logs.